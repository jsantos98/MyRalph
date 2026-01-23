using ProjectManagement.Core.Enums;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Interface for state transition validation
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// Validates if a WorkItem can transition to the target status
    /// </summary>
    bool CanTransition(WorkItemStatus current, WorkItemStatus target);

    /// <summary>
    /// Validates if a DeveloperStory can transition to the target status
    /// </summary>
    bool CanTransition(DeveloperStoryStatus current, DeveloperStoryStatus target);

    /// <summary>
    /// Gets the valid next states for a WorkItem
    /// </summary>
    IEnumerable<WorkItemStatus> GetValidTransitions(WorkItemStatus current);

    /// <summary>
    /// Gets the valid next states for a DeveloperStory
    /// </summary>
    IEnumerable<DeveloperStoryStatus> GetValidTransitions(DeveloperStoryStatus current);
}
