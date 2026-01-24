using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Claude;
using ProjectManagement.Application.Services;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Service for refining work items into developer stories
/// </summary>
public class RefinementService : IRefinementService
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IDeveloperStoryRepository _developerStoryRepository;
    private readonly IDeveloperStoryDependencyRepository _dependencyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStateManager _stateManager;
    private readonly IClaudeApiService _claudeApiService;
    private readonly ILogger<RefinementService> _logger;

    public RefinementService(
        IWorkItemRepository workItemRepository,
        IDeveloperStoryRepository developerStoryRepository,
        IDeveloperStoryDependencyRepository dependencyRepository,
        IUnitOfWork unitOfWork,
        IStateManager stateManager,
        IClaudeApiService claudeApiService,
        ILogger<RefinementService> logger)
    {
        _workItemRepository = workItemRepository;
        _developerStoryRepository = developerStoryRepository;
        _dependencyRepository = dependencyRepository;
        _unitOfWork = unitOfWork;
        _stateManager = stateManager;
        _claudeApiService = claudeApiService;
        _logger = logger;
    }

    public async Task<RefinementResult> RefineWorkItemAsync(
        int workItemId,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        // Get the work item with its stories
        var workItem = await _workItemRepository.GetWithDeveloperStoriesAsync(workItemId, cancellationToken);
        if (workItem == null)
        {
            throw new EntityNotFoundException(typeof(WorkItem), workItemId);
        }

        // Validate state
        if (!_stateManager.CanTransition(workItem.Status, WorkItemStatus.Refining))
        {
            throw new InvalidStateTransitionException(workItem.Status, WorkItemStatus.Refining, typeof(WorkItem));
        }

        // Update to Refining
        workItem.UpdateStatus(WorkItemStatus.Refining);
        await _workItemRepository.UpdateAsync(workItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        try
        {
            // Call Claude to refine the work item
            var claudeResult = await _claudeApiService.RefineWorkItemAsync(
                workItem,
                apiKey,
                baseUrl,
                timeoutMs,
                model,
                cancellationToken);

            // Create developer stories
            var stories = new List<DeveloperStory>();
            var storyMap = new Dictionary<int, DeveloperStory>();

            foreach (var storyGen in claudeResult.DeveloperStories)
            {
                var story = new DeveloperStory
                {
                    WorkItemId = workItem.Id,
                    StoryType = (DeveloperStoryType)storyGen.StoryType,
                    Title = storyGen.Title,
                    Description = storyGen.Description,
                    Instructions = storyGen.Instructions,
                    Priority = workItem.Priority,
                    Status = DeveloperStoryStatus.Pending
                };

                await _developerStoryRepository.AddAsync(story, cancellationToken);
                stories.Add(story);
                storyMap[stories.Count - 1] = story;
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            // Create dependencies
            var dependencies = new List<DeveloperStoryDependency>();
            foreach (var dep in claudeResult.Dependencies)
            {
                if (storyMap.TryGetValue(dep.DependentStoryIndex, out var dependentStory) &&
                    storyMap.TryGetValue(dep.RequiredStoryIndex, out var requiredStory))
                {
                    var dependency = new DeveloperStoryDependency
                    {
                        DependentStoryId = dependentStory.Id,
                        RequiredStoryId = requiredStory.Id,
                        Description = dep.Description
                    };

                    await _dependencyRepository.AddAsync(dependency, cancellationToken);
                    dependencies.Add(dependency);
                }
            }

            // Update story statuses based on dependencies
            foreach (var story in stories)
            {
                var storyWithDeps = await _developerStoryRepository.GetWithDependenciesAsync(story.Id, cancellationToken);
                if (storyWithDeps?.Dependencies.Any() == true)
                {
                    storyWithDeps.UpdateStatus(DeveloperStoryStatus.Blocked);
                    await _developerStoryRepository.UpdateAsync(storyWithDeps, cancellationToken);
                }
                else
                {
                    storyWithDeps!.UpdateStatus(DeveloperStoryStatus.Ready);
                    await _developerStoryRepository.UpdateAsync(storyWithDeps, cancellationToken);
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            // Update work item to Refined
            workItem.UpdateStatus(WorkItemStatus.Refined);
            await _workItemRepository.UpdateAsync(workItem, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Refined WorkItem {Id} into {Count} developer stories",
                workItemId, stories.Count);

            return new RefinementResult
            {
                WorkItem = workItem,
                DeveloperStories = stories,
                Dependencies = dependencies,
                Analysis = claudeResult.Analysis
            };
        }
        catch (Exception ex)
        {
            // Update work item to Error state
            workItem.UpdateStatus(WorkItemStatus.Error, ex.Message);
            await _workItemRepository.UpdateAsync(workItem, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogError(ex, "Failed to refine WorkItem {Id}", workItemId);
            throw;
        }
    }

    public async Task<DeveloperStory> AddDeveloperStoryAsync(
        int workItemId,
        int storyType,
        string title,
        string description,
        string instructions,
        CancellationToken cancellationToken = default)
    {
        var workItem = await _workItemRepository.GetByIdAsync(workItemId, cancellationToken);
        if (workItem == null)
        {
            throw new EntityNotFoundException(typeof(WorkItem), workItemId);
        }

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = (DeveloperStoryType)storyType,
            Title = title,
            Description = description,
            Instructions = instructions,
            Priority = workItem.Priority,
            Status = DeveloperStoryStatus.Ready
        };

        await _developerStoryRepository.AddAsync(story, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Manually added DeveloperStory {Id} to WorkItem {WorkItemId}",
            story.Id, workItemId);

        return story;
    }
}
