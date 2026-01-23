namespace ProjectManagement.Core.Enums;

/// <summary>
/// Represents the status of a DeveloperStory
/// </summary>
public enum DeveloperStoryStatus
{
    /// <summary>
    /// Initial state, created but not yet ready
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Ready to be worked on (dependencies resolved)
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Currently being implemented
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Implementation completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// An error occurred during processing
    /// </summary>
    Error = 4,

    /// <summary>
    /// Blocked by dependencies or other issues
    /// </summary>
    Blocked = 5
}
