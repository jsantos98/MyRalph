using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;

namespace ProjectManagement.Core.Interfaces;

/// <summary>
/// Repository interface for WorkItem entity
/// </summary>
public interface IWorkItemRepository : IRepository<WorkItem>
{
    /// <summary>
    /// Gets a work item with its developer stories
    /// </summary>
    Task<WorkItem?> GetWithDeveloperStoriesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items by status
    /// </summary>
    Task<IEnumerable<WorkItem>> GetByStatusAsync(
        WorkItemStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items by type
    /// </summary>
    Task<IEnumerable<WorkItem>> GetByTypeAsync(
        WorkItemType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the work item currently in progress (if any)
    /// </summary>
    Task<WorkItem?> GetInProgressAsync(CancellationToken cancellationToken = default);
}
