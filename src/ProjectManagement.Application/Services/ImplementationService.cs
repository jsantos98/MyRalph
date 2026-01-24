using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Claude;
using ProjectManagement.Infrastructure.Git;
using ProjectManagement.Application.Services;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Service for orchestrating developer story implementation
/// </summary>
public class ImplementationService : IImplementationService
{
    private readonly IDeveloperStoryRepository _developerStoryRepository;
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IExecutionLogRepository _executionLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStateManager _stateManager;
    private readonly IClaudeCodeIntegration _claudeCodeIntegration;
    private readonly IGitService _gitService;
    private readonly GitSettings _gitSettings;
    private readonly ILogger<ImplementationService> _logger;

    public ImplementationService(
        IDeveloperStoryRepository developerStoryRepository,
        IWorkItemRepository workItemRepository,
        IExecutionLogRepository executionLogRepository,
        IUnitOfWork unitOfWork,
        IStateManager stateManager,
        IClaudeCodeIntegration claudeCodeIntegration,
        IGitService gitService,
        IOptions<GitSettings> gitSettings,
        ILogger<ImplementationService> logger)
    {
        _developerStoryRepository = developerStoryRepository;
        _workItemRepository = workItemRepository;
        _executionLogRepository = executionLogRepository;
        _unitOfWork = unitOfWork;
        _stateManager = stateManager;
        _claudeCodeIntegration = claudeCodeIntegration;
        _gitService = gitService;
        _gitSettings = gitSettings.Value;
        _logger = logger;
    }

    public async Task<ImplementationResult> ImplementAsync(
        int developerStoryId,
        string mainBranch,
        string repositoryPath,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var story = await _developerStoryRepository.GetWithDependenciesAsync(developerStoryId, cancellationToken);
        if (story == null)
        {
            throw new EntityNotFoundException(typeof(DeveloperStory), developerStoryId);
        }

        // Validate state
        if (!_stateManager.CanTransition(story.Status, DeveloperStoryStatus.InProgress))
        {
            throw new InvalidStateTransitionException(story.Status, DeveloperStoryStatus.InProgress, typeof(DeveloperStory));
        }

        // Check if this is the first story for the work item
        var workItem = await _workItemRepository.GetByIdAsync(story.WorkItemId, cancellationToken);
        if (workItem == null)
        {
            throw new EntityNotFoundException(typeof(WorkItem), story.WorkItemId);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new ImplementationResult { Story = story };

        try
        {
            // Update work item to InProgress if it's the first story
            if (workItem.Status == WorkItemStatus.Refined)
            {
                workItem.UpdateStatus(WorkItemStatus.InProgress);
                await _workItemRepository.UpdateAsync(workItem, cancellationToken);
            }

            // Create branch if this is the first story for the work item
            if (string.IsNullOrEmpty(story.GitBranch))
            {
                var branchName = await EnsureBranchForWorkItemAsync(story.WorkItemId, repositoryPath, mainBranch, cancellationToken);
                story.GitBranch = branchName;
            }

            // Create worktree
            var worktreePath = _gitService.GetWorktreePath(story, Path.GetFullPath(_gitSettings.WorktreeBasePath));

            if (!await _gitService.WorktreeExistsAsync(repositoryPath, worktreePath, cancellationToken))
            {
                await _gitService.CreateWorktreeAsync(repositoryPath, story.GitBranch!, worktreePath, cancellationToken);
                await _executionLogRepository.AddLogAsync(
                    story.Id,
                    ExecutionEventType.WorktreeCreated,
                    $"Created worktree at {worktreePath}",
                    cancellationToken: cancellationToken);
            }

            story.GitWorktree = worktreePath;

            // Mark as InProgress
            story.UpdateStatus(DeveloperStoryStatus.InProgress);
            await _developerStoryRepository.UpdateAsync(story, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await _executionLogRepository.AddLogAsync(
                story.Id,
                ExecutionEventType.Started,
                $"Started implementation: {story.Title}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Starting implementation for DeveloperStory {Id}", story.Id);

            // Execute Claude Code
            var claudeResult = await _claudeCodeIntegration.ExecuteAsync(
                story.Instructions,
                worktreePath,
                apiKey,
                baseUrl,
                timeoutMs,
                model,
                cancellationToken);

            result.Output = claudeResult.StandardOutput;
            result.Duration = claudeResult.Duration;

            if (claudeResult.Success)
            {
                // Mark as Completed
                story.UpdateStatus(DeveloperStoryStatus.Completed);
                await _developerStoryRepository.UpdateAsync(story, cancellationToken);

                await _executionLogRepository.AddLogAsync(
                    story.Id,
                    ExecutionEventType.Completed,
                    $"Implementation completed successfully in {claudeResult.Duration}",
                    cancellationToken: cancellationToken);

                // Remove worktree
                try
                {
                    await _gitService.RemoveWorktreeAsync(repositoryPath, worktreePath, cancellationToken);
                    story.GitWorktree = null;
                    await _developerStoryRepository.UpdateAsync(story, cancellationToken);
                    await _executionLogRepository.AddLogAsync(
                        story.Id,
                        ExecutionEventType.WorktreeRemoved,
                        $"Removed worktree at {worktreePath}",
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove worktree for story {Id}", story.Id);
                }

                // Check if all stories for the work item are completed
                await CheckWorkItemCompletionAsync(workItem, cancellationToken);

                result.Success = true;
                _logger.LogInformation("Successfully implemented DeveloperStory {Id}", story.Id);
            }
            else
            {
                // Mark as Error
                story.UpdateStatus(DeveloperStoryStatus.Error, claudeResult.StandardError);
                await _developerStoryRepository.UpdateAsync(story, cancellationToken);

                await _executionLogRepository.AddLogAsync(
                    story.Id,
                    ExecutionEventType.Failed,
                    null,
                    claudeResult.StandardError,
                    cancellationToken: cancellationToken);

                result.Error = claudeResult.StandardError;
                _logger.LogError("Failed to implement DeveloperStory {Id}: {Error}", story.Id, claudeResult.StandardError);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            story.UpdateStatus(DeveloperStoryStatus.Error, "Implementation was cancelled");
            await _developerStoryRepository.UpdateAsync(story, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            story.UpdateStatus(DeveloperStoryStatus.Error, ex.Message);
            await _developerStoryRepository.UpdateAsync(story, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogError(ex, "Error implementing DeveloperStory {Id}", story.Id);
            result.Error = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<string> EnsureBranchForWorkItemAsync(
        int workItemId,
        string repositoryPath,
        string mainBranch,
        CancellationToken cancellationToken = default)
    {
        var workItem = await _workItemRepository.GetByIdAsync(workItemId, cancellationToken);
        if (workItem == null)
        {
            throw new EntityNotFoundException(typeof(WorkItem), workItemId);
        }

        var branchName = workItem.DefaultBranchName;

        if (!await _gitService.BranchExistsAsync(repositoryPath, branchName, cancellationToken))
        {
            await _gitService.CreateBranchAsync(repositoryPath, branchName, mainBranch, cancellationToken);
            _logger.LogInformation("Created branch {Branch} for WorkItem {WorkItemId}", branchName, workItemId);
        }

        return branchName;
    }

    private async Task CheckWorkItemCompletionAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        var stories = await _developerStoryRepository.GetByWorkItemIdAsync(workItem.Id, cancellationToken);

        if (stories.All(s => s.Status == DeveloperStoryStatus.Completed))
        {
            workItem.UpdateStatus(WorkItemStatus.Completed);
            await _workItemRepository.UpdateAsync(workItem, cancellationToken);
            _logger.LogInformation("WorkItem {Id} marked as completed - all stories done", workItem.Id);
        }
    }
}
