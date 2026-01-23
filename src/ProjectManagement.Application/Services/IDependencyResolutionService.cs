using ProjectManagement.Core.Entities;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Interface for dependency resolution and next story selection
/// </summary>
public interface IDependencyResolutionService
{
    /// <summary>
    /// Selects the next available developer story for implementation
    /// </summary>
    Task<DeveloperStory?> SelectNextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stories that are blocked with their blockers
    /// </summary>
    Task<Dictionary<DeveloperStory, List<DeveloperStory>>> GetBlockedStoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates dependency status for all stories
    /// </summary>
    Task UpdateDependencyStatusesAsync(CancellationToken cancellationToken = default);
}
