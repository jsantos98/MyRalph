using ProjectManagement.Core.Enums;

namespace ProjectManagement.Core.Entities;

/// <summary>
/// Represents an audit log entry for developer story execution
/// </summary>
public class ExecutionLog : Entity
{
    /// <summary>
    /// The developer story this log entry is for
    /// </summary>
    public int DeveloperStoryId { get; set; }

    /// <summary>
    /// Navigation property to the developer story
    /// </summary>
    public DeveloperStory? DeveloperStory { get; set; }

    /// <summary>
    /// The type of event that occurred
    /// </summary>
    public ExecutionEventType EventType { get; set; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Detailed information about the event
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Error message if the event was a failure
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}
