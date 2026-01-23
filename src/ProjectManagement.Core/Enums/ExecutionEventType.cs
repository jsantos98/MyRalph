namespace ProjectManagement.Core.Enums;

/// <summary>
/// Represents the type of event in an ExecutionLog
/// </summary>
public enum ExecutionEventType
{
    /// <summary>
    /// Story started processing
    /// </summary>
    Started = 0,

    /// <summary>
    /// Story completed successfully
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Story failed with error
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Story was retried
    /// </summary>
    Retried = 3,

    /// <summary>
    /// Branch created for story
    /// </summary>
    BranchCreated = 4,

    /// <summary>
    /// Worktree created for story
    /// </summary>
    WorktreeCreated = 5,

    /// <summary>
    /// Worktree removed after completion
    /// </summary>
    WorktreeRemoved = 6,

    /// <summary>
    /// Generic information log
    /// </summary>
    Info = 7
}
