using ProjectManagement.Core.Entities;

namespace ProjectManagement.Core.Interfaces;

/// <summary>
/// Repository interface for DeveloperStoryDependency entity
/// </summary>
public interface IDeveloperStoryDependencyRepository : IRepository<DeveloperStoryDependency>
{
    /// <summary>
    /// Gets all dependencies for a story (stories that this story requires)
    /// </summary>
    Task<IEnumerable<DeveloperStoryDependency>> GetDependenciesForStoryAsync(
        int developerStoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all dependent stories (stories that require this story)
    /// </summary>
    Task<IEnumerable<DeveloperStoryDependency>> GetDependentStoriesAsync(
        int developerStoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific dependency relationship exists
    /// </summary>
    Task<bool> DependencyExistsAsync(
        int dependentStoryId,
        int requiredStoryId,
        CancellationToken cancellationToken = default);
}
