using ProjectManagement.Core.Entities;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Interface for refining work items into developer stories
/// </summary>
public interface IRefinementService
{
    /// <summary>
    /// Refines a work item into developer stories using Claude
    /// </summary>
    Task<RefinementResult> RefineWorkItemAsync(
        int workItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually adds a developer story to a work item
    /// </summary>
    Task<DeveloperStory> AddDeveloperStoryAsync(
        int workItemId,
        int storyType,
        string title,
        string description,
        string instructions,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a refinement operation
/// </summary>
public class RefinementResult
{
    /// <summary>
    /// The work item that was refined
    /// </summary>
    public required WorkItem WorkItem { get; set; }

    /// <summary>
    /// Developer stories created
    /// </summary>
    public required List<DeveloperStory> DeveloperStories { get; set; }

    /// <summary>
    /// Dependencies created between stories
    /// </summary>
    public required List<DeveloperStoryDependency> Dependencies { get; set; }

    /// <summary>
    /// Analysis from Claude (if available)
    /// </summary>
    public string? Analysis { get; set; }
}
