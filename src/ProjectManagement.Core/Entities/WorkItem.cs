using ProjectManagement.Core.Enums;

namespace ProjectManagement.Core.Entities;

/// <summary>
/// Represents a User Story or Bug in the project management system
/// </summary>
public class WorkItem : Entity
{
    /// <summary>
    /// The type of work item (UserStory or Bug)
    /// </summary>
    public WorkItemType Type { get; set; }

    /// <summary>
    /// Title of the work item
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the work item
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional acceptance criteria for the work item
    /// </summary>
    public string? AcceptanceCriteria { get; set; }

    /// <summary>
    /// Priority level (1-9, where 1 is highest priority)
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Current status of the work item
    /// </summary>
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Pending;

    /// <summary>
    /// Optional error message if status is Error
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the work item was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the work item was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to child developer stories
    /// </summary>
    public ICollection<DeveloperStory> DeveloperStories { get; set; } = new List<DeveloperStory>();

    /// <summary>
    /// Gets the branch prefix for this work item type
    /// </summary>
    public string BranchPrefix => Type == WorkItemType.UserStory ? "us" : "bug";

    /// <summary>
    /// Gets the default branch name for this work item
    /// </summary>
    public string DefaultBranchName => $"{BranchPrefix}-{Id}";

    /// <summary>
    /// Updates the status and sets the UpdatedAt timestamp
    /// </summary>
    public void UpdateStatus(WorkItemStatus newStatus, string? errorMessage = null)
    {
        Status = newStatus;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the work item can transition to the specified status
    /// </summary>
    public bool CanTransitionTo(WorkItemStatus newStatus)
    {
        return (Status, newStatus) switch
        {
            (WorkItemStatus.Pending, WorkItemStatus.Refining) => true,
            (WorkItemStatus.Pending, WorkItemStatus.Error) => true,
            (WorkItemStatus.Refining, WorkItemStatus.Refined) => true,
            (WorkItemStatus.Refining, WorkItemStatus.Error) => true,
            (WorkItemStatus.Refined, WorkItemStatus.InProgress) => true,
            (WorkItemStatus.Refined, WorkItemStatus.Error) => true,
            (WorkItemStatus.InProgress, WorkItemStatus.Completed) => true,
            (WorkItemStatus.InProgress, WorkItemStatus.Error) => true,
            (WorkItemStatus.Error, WorkItemStatus.Pending) => true,
            (WorkItemStatus.Error, WorkItemStatus.Refining) => true,
            _ => false
        };
    }
}
