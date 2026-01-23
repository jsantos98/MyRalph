using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;

namespace ProjectManagement.Core.Interfaces;

/// <summary>
/// Repository interface for ExecutionLog entity
/// </summary>
public interface IExecutionLogRepository : IRepository<ExecutionLog>
{
    /// <summary>
    /// Gets all execution logs for a developer story
    /// </summary>
    Task<IEnumerable<ExecutionLog>> GetByDeveloperStoryIdAsync(
        int developerStoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution logs by event type
    /// </summary>
    Task<IEnumerable<ExecutionLog>> GetByEventTypeAsync(
        ExecutionEventType eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an execution log entry
    /// </summary>
    Task<ExecutionLog> AddLogAsync(
        int developerStoryId,
        ExecutionEventType eventType,
        string? details = null,
        string? errorMessage = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
