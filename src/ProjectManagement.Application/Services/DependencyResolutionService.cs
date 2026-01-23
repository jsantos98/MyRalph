using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Application.Services;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Service for resolving dependencies and selecting next stories
/// </summary>
public class DependencyResolutionService : IDependencyResolutionService
{
    private readonly IDeveloperStoryRepository _developerStoryRepository;
    private readonly IDeveloperStoryDependencyRepository _dependencyRepository;
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DependencyResolutionService> _logger;

    public DependencyResolutionService(
        IDeveloperStoryRepository developerStoryRepository,
        IDeveloperStoryDependencyRepository dependencyRepository,
        IWorkItemRepository workItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<DependencyResolutionService> logger)
    {
        _developerStoryRepository = developerStoryRepository;
        _dependencyRepository = dependencyRepository;
        _workItemRepository = workItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DeveloperStory?> SelectNextAsync(CancellationToken cancellationToken = default)
    {
        // Business rule: Only one UserStory can be InProgress at a time
        var inProgressWorkItem = await _workItemRepository.GetInProgressAsync(cancellationToken);
        if (inProgressWorkItem != null && inProgressWorkItem.Type == WorkItemType.UserStory)
        {
            _logger.LogInformation("A UserStory is already in progress (ID: {Id})", inProgressWorkItem.Id);
            return null;
        }

        // Get all Ready stories with resolved dependencies
        var readyStories = await _developerStoryRepository.GetReadyWithResolvedDependenciesAsync(cancellationToken);
        var readyList = readyStories.ToList();

        if (!readyList.Any())
        {
            _logger.LogInformation("No ready stories available");
            return null;
        }

        // Load work items for priority sorting
        var workItemIds = readyList.Select(s => s.WorkItemId).Distinct().ToList();
        var workItems = new Dictionary<int, WorkItem>();
        foreach (var workItemId in workItemIds)
        {
            var wi = await _workItemRepository.GetByIdAsync(workItemId, cancellationToken);
            if (wi != null) workItems[workItemId] = wi;
        }

        // Sort by Priority (ascending), then WorkItem.Priority, then Story ID (FIFO)
        var selected = readyList
            .OrderBy(s => s.Priority)
            .ThenBy(s => workItems.GetValueOrDefault(s.WorkItemId)?.Priority ?? 5)
            .ThenBy(s => s.Id)
            .FirstOrDefault();

        if (selected != null)
        {
            _logger.LogInformation("Selected DeveloperStory {Id} for implementation", selected.Id);
        }

        return selected;
    }

    public async Task<Dictionary<DeveloperStory, List<DeveloperStory>>> GetBlockedStoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var blockedStories = await _developerStoryRepository.GetBlockedStoriesAsync(cancellationToken);
        var result = new Dictionary<DeveloperStory, List<DeveloperStory>>();

        foreach (var story in blockedStories)
        {
            var storyWithDeps = await _developerStoryRepository.GetWithDependenciesAsync(story.Id, cancellationToken);
            if (storyWithDeps == null) continue;

            var blockers = new List<DeveloperStory>();
            foreach (var dep in storyWithDeps.Dependencies)
            {
                if (dep.RequiredStory?.Status != DeveloperStoryStatus.Completed)
                {
                    blockers.Add(dep.RequiredStory!);
                }
            }

            result[story] = blockers;
        }

        return result;
    }

    public async Task UpdateDependencyStatusesAsync(CancellationToken cancellationToken = default)
    {
        // Get all Pending and Blocked stories
        var pendingStories = await _developerStoryRepository.GetByStatusAsync(
            DeveloperStoryStatus.Pending, cancellationToken);
        var blockedStories = await _developerStoryRepository.GetByStatusAsync(
            DeveloperStoryStatus.Blocked, cancellationToken);

        var allStoriesToCheck = pendingStories.Concat(blockedStories).ToList();

        foreach (var story in allStoriesToCheck)
        {
            var storyWithDeps = await _developerStoryRepository.GetWithDependenciesAsync(story.Id, cancellationToken);
            if (storyWithDeps == null) continue;

            bool hasIncompleteDependencies = false;
            foreach (var dep in storyWithDeps.Dependencies)
            {
                if (dep.RequiredStory?.Status != DeveloperStoryStatus.Completed)
                {
                    hasIncompleteDependencies = true;
                    break;
                }
            }

            DeveloperStoryStatus newStatus;
            if (storyWithDeps.Dependencies.Any() && hasIncompleteDependencies)
            {
                newStatus = DeveloperStoryStatus.Blocked;
            }
            else if (!storyWithDeps.Dependencies.Any())
            {
                newStatus = DeveloperStoryStatus.Ready;
            }
            else
            {
                newStatus = DeveloperStoryStatus.Ready;
            }

            if (storyWithDeps.Status != newStatus)
            {
                storyWithDeps.UpdateStatus(newStatus);
                await _developerStoryRepository.UpdateAsync(storyWithDeps, cancellationToken);
                _logger.LogDebug("Updated story {Id} status to {Status}", storyWithDeps.Id, newStatus);
            }
        }

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
