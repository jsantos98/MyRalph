using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Interface for WorkItem business operations
/// </summary>
public interface IWorkItemService
{
    /// <summary>
    /// Creates a new work item
    /// </summary>
    Task<WorkItem> CreateAsync(
        WorkItemType type,
        string title,
        string description,
        string? acceptanceCriteria,
        int priority,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a work item by ID
    /// </summary>
    Task<WorkItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a work item with all developer stories
    /// </summary>
    Task<WorkItem?> GetWithDeveloperStoriesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all work items
    /// </summary>
    Task<IEnumerable<WorkItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items by status
    /// </summary>
    Task<IEnumerable<WorkItem>> GetByStatusAsync(
        WorkItemStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a work item
    /// </summary>
    Task UpdateStatusAsync(
        int id,
        WorkItemStatus newStatus,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a UserStory is currently in progress (business rule: only one at a time)
    /// </summary>
    Task<bool> HasInProgressUserStoryAsync(CancellationToken cancellationToken = default);
}
