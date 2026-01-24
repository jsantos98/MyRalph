using ProjectManagement.Core.Entities;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Interface for developer story implementation orchestration
/// </summary>
public interface IImplementationService
{
    /// <summary>
    /// Implements a developer story using Claude Code
    /// </summary>
    Task<ImplementationResult> ImplementAsync(
        int developerStoryId,
        string mainBranch,
        string repositoryPath,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the git branch for a work item (if not exists)
    /// </summary>
    Task<string> EnsureBranchForWorkItemAsync(
        int workItemId,
        string repositoryPath,
        string mainBranch,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an implementation operation
/// </summary>
public class ImplementationResult
{
    /// <summary>
    /// The developer story that was implemented
    /// </summary>
    public required DeveloperStory Story { get; set; }

    /// <summary>
    /// Whether implementation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Output from Claude Code
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Duration of implementation
    /// </summary>
    public TimeSpan Duration { get; set; }
}
