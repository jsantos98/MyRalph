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

        // Validate state - allow re-refinement if already in Refining state
        if (workItem.Status != WorkItemStatus.Refining &&
            !_stateManager.CanTransition(workItem.Status, WorkItemStatus.Refining))
        {
            throw new InvalidStateTransitionException(workItem.Status, WorkItemStatus.Refining, typeof(WorkItem));
        }

        // Update to Refining only if not already in that state
        if (workItem.Status != WorkItemStatus.Refining)
        {
            workItem.UpdateStatus(WorkItemStatus.Refining);
            await _workItemRepository.UpdateAsync(workItem, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

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

            // Log the story IDs after commit
            _logger.LogInformation("Stories created with database IDs:");
            for (int i = 0; i < stories.Count; i++)
            {
                _logger.LogInformation("  Index {Index}: Story ID = {StoryId}, Title = {Title}",
                    i, stories[i].Id, stories[i].Title);
            }

            // Create dependencies
            var dependencies = new List<DeveloperStoryDependency>();
            _logger.LogInformation("Processing {Count} dependencies from Claude", claudeResult.Dependencies.Count);

            foreach (var dep in claudeResult.Dependencies)
            {
                // Skip self-blocking dependencies (a story depending on itself)
                // Check at the index level before looking up the actual stories
                if (dep.DependentStoryIndex == dep.RequiredStoryIndex)
                {
                    _logger.LogWarning("Skipping self-blocking dependency at index {Index}", dep.DependentStoryIndex);
                    continue;
                }

                // Validate indices are within range
                if (dep.DependentStoryIndex < 0 || dep.DependentStoryIndex >= stories.Count ||
                    dep.RequiredStoryIndex < 0 || dep.RequiredStoryIndex >= stories.Count)
                {
                    _logger.LogError("Invalid dependency indices: Dependent={Dependent}, Required={Required}, StoryCount={Count}",
                        dep.DependentStoryIndex, dep.RequiredStoryIndex, stories.Count);
                    continue;
                }

                if (storyMap.TryGetValue(dep.DependentStoryIndex, out var dependentStory) &&
                    storyMap.TryGetValue(dep.RequiredStoryIndex, out var requiredStory))
                {
                    // Skip duplicate dependencies
                    if (dependencies.Any(d =>
                        d.DependentStoryId == dependentStory.Id &&
                        d.RequiredStoryId == requiredStory.Id))
                    {
                        _logger.LogWarning("Skipping duplicate dependency: Story {DependentId} -> Story {RequiredId}",
                            dependentStory.Id, requiredStory.Id);
                        continue;
                    }

                    _logger.LogInformation("Creating dependency: Story {DependentId} (index {DependentIndex}) -> Story {RequiredId} (index {RequiredIndex})",
                        dependentStory.Id, dep.DependentStoryIndex, requiredStory.Id, dep.RequiredStoryIndex);

                    var dependency = new DeveloperStoryDependency
                    {
                        DependentStoryId = dependentStory.Id,
                        RequiredStoryId = requiredStory.Id,
                        Description = dep.Description
                    };

                    await _dependencyRepository.AddAsync(dependency, cancellationToken);
                    dependencies.Add(dependency);
                }
                else
                {
                    _logger.LogError("Failed to find stories for dependency: Dependent index {DependentIndex}, Required index {RequiredIndex}",
                        dep.DependentStoryIndex, dep.RequiredStoryIndex);
                }
            }

            // Update story statuses based on in-memory dependencies (not yet committed)
            foreach (var story in stories)
            {
                // Check if this story has any incoming dependencies from the in-memory list
                var hasBlockingDependencies = dependencies.Any(d => d.DependentStoryId == story.Id);

                if (hasBlockingDependencies)
                {
                    story.UpdateStatus(DeveloperStoryStatus.Blocked);
                    _logger.LogInformation("Story {StoryId} ({Title}) is Blocked by dependencies", story.Id, story.Title);
                }
                else
                {
                    story.UpdateStatus(DeveloperStoryStatus.Ready);
                    _logger.LogInformation("Story {StoryId} ({Title}) is Ready (no blocking dependencies)", story.Id, story.Title);
                }

                await _developerStoryRepository.UpdateAsync(story, cancellationToken);
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
