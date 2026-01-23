namespace ProjectManagement.Core.Entities;

/// <summary>
/// Represents a dependency relationship between developer stories
/// </summary>
public class DeveloperStoryDependency : Entity
{
    /// <summary>
    /// The story that has the dependency (needs the RequiredStory to complete first)
    /// </summary>
    public int DependentStoryId { get; set; }

    /// <summary>
    /// Navigation property to the dependent story
    /// </summary>
    public DeveloperStory? DependentStory { get; set; }

    /// <summary>
    /// The story that must complete first
    /// </summary>
    public int RequiredStoryId { get; set; }

    /// <summary>
    /// Navigation property to the required story
    /// </summary>
    public DeveloperStory? RequiredStory { get; set; }

    /// <summary>
    /// Optional description of why this dependency exists
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when the dependency was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
