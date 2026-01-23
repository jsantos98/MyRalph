using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;

namespace ProjectManagement.Core.Interfaces;

/// <summary>
/// Repository interface for DeveloperStory entity
/// </summary>
public interface IDeveloperStoryRepository : IRepository<DeveloperStory>
{
    /// <summary>
    /// Gets a developer story with its dependencies
    /// </summary>
    Task<DeveloperStory?> GetWithDependenciesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a developer story with its execution logs
    /// </summary>
    Task<DeveloperStory?> GetWithExecutionLogsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets developer stories by work item ID
    /// </summary>
    Task<IEnumerable<DeveloperStory>> GetByWorkItemIdAsync(
        int workItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets developer stories by status
    /// </summary>
    Task<IEnumerable<DeveloperStory>> GetByStatusAsync(
        DeveloperStoryStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ready stories (status == Ready) with all dependencies completed
    /// </summary>
    Task<IEnumerable<DeveloperStory>> GetReadyWithResolvedDependenciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stories that are blocked by incomplete dependencies
    /// </summary>
    Task<IEnumerable<DeveloperStory>> GetBlockedStoriesAsync(
        CancellationToken cancellationToken = default);
}
