using ProjectManagement.Core.Entities;

namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Interface for Claude API service for refinement operations
/// </summary>
public interface IClaudeApiService
{
    /// <summary>
    /// Refines a work item into developer stories using Claude API
    /// </summary>
    Task<ClaudeRefinementResult> RefineWorkItemAsync(
        WorkItem workItem,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Claude refinement operation
/// </summary>
public class ClaudeRefinementResult
{
    /// <summary>
    /// Generated developer stories
    /// </summary>
    public List<DeveloperStoryGeneration> DeveloperStories { get; set; } = new();

    /// <summary>
    /// Generated dependencies between stories
    /// </summary>
    public List<StoryDependency> Dependencies { get; set; } = new();

    /// <summary>
    /// Overall analysis or explanation from Claude
    /// </summary>
    public string? Analysis { get; set; }
}

/// <summary>
/// Represents a generated developer story
/// </summary>
public class DeveloperStoryGeneration
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Instructions { get; set; }
    public int StoryType { get; set; } // Implementation=0, UnitTests=1, FeatureTests=2, Documentation=3
}

/// <summary>
/// Represents a dependency between stories
/// </summary>
public class StoryDependency
{
    public int DependentStoryIndex { get; set; }
    public int RequiredStoryIndex { get; set; }
    public string? Description { get; set; }
}
