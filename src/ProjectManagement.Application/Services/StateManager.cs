using ProjectManagement.Core.Enums;

namespace ProjectManagement.Application.Services;

/// <summary>
/// State transition validation service
/// </summary>
public class StateManager : IStateManager
{
    private static readonly HashSet<(WorkItemStatus, WorkItemStatus)> ValidWorkItemTransitions = new()
    {
        (WorkItemStatus.Pending, WorkItemStatus.Refining),
        (WorkItemStatus.Pending, WorkItemStatus.Error),
        (WorkItemStatus.Refining, WorkItemStatus.Refined),
        (WorkItemStatus.Refining, WorkItemStatus.Error),
        (WorkItemStatus.Refined, WorkItemStatus.InProgress),
        (WorkItemStatus.Refined, WorkItemStatus.Error),
        (WorkItemStatus.InProgress, WorkItemStatus.Completed),
        (WorkItemStatus.InProgress, WorkItemStatus.Error),
        (WorkItemStatus.Error, WorkItemStatus.Pending),
        (WorkItemStatus.Error, WorkItemStatus.Refining)
    };

    private static readonly HashSet<(DeveloperStoryStatus, DeveloperStoryStatus)> ValidDeveloperStoryTransitions = new()
    {
        (DeveloperStoryStatus.Pending, DeveloperStoryStatus.Ready),
        (DeveloperStoryStatus.Pending, DeveloperStoryStatus.Blocked),
        (DeveloperStoryStatus.Pending, DeveloperStoryStatus.Error),
        (DeveloperStoryStatus.Ready, DeveloperStoryStatus.InProgress),
        (DeveloperStoryStatus.Ready, DeveloperStoryStatus.Blocked),
        (DeveloperStoryStatus.Ready, DeveloperStoryStatus.Error),
        (DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Completed),
        (DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Error),
        (DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Blocked),
        (DeveloperStoryStatus.Blocked, DeveloperStoryStatus.Ready),
        (DeveloperStoryStatus.Blocked, DeveloperStoryStatus.Error),
        (DeveloperStoryStatus.Error, DeveloperStoryStatus.Pending),
        (DeveloperStoryStatus.Error, DeveloperStoryStatus.Ready)
    };

    public bool CanTransition(WorkItemStatus current, WorkItemStatus target)
    {
        return ValidWorkItemTransitions.Contains((current, target));
    }

    public bool CanTransition(DeveloperStoryStatus current, DeveloperStoryStatus target)
    {
        return ValidDeveloperStoryTransitions.Contains((current, target));
    }

    public IEnumerable<WorkItemStatus> GetValidTransitions(WorkItemStatus current)
    {
        return ValidWorkItemTransitions
            .Where(t => t.Item1 == current)
            .Select(t => t.Item2);
    }

    public IEnumerable<DeveloperStoryStatus> GetValidTransitions(DeveloperStoryStatus current)
    {
        return ValidDeveloperStoryTransitions
            .Where(t => t.Item1 == current)
            .Select(t => t.Item2);
    }
}
