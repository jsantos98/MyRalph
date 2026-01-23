namespace ProjectManagement.Core.Enums;

/// <summary>
/// Represents the status of a WorkItem
/// </summary>
public enum WorkItemStatus
{
    /// <summary>
    /// Initial state when a work item is created
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Currently being refined into developer stories
    /// </summary>
    Refining = 1,

    /// <summary>
    /// Successfully refined into developer stories
    /// </summary>
    Refined = 2,

    /// <summary>
    /// Currently being implemented
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Implementation completed successfully
    /// </summary>
    Completed = 4,

    /// <summary>
    /// An error occurred during processing
    /// </summary>
    Error = 5
}
