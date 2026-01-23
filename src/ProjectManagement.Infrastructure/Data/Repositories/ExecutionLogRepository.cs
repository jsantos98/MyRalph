using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Data.DbContext;

namespace ProjectManagement.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for ExecutionLog entity
/// </summary>
public class ExecutionLogRepository : Repository<ExecutionLog>, IExecutionLogRepository
{
    public ExecutionLogRepository(ProjectManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ExecutionLog>> GetByDeveloperStoryIdAsync(
        int developerStoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(el => el.DeveloperStoryId == developerStoryId)
            .OrderByDescending(el => el.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExecutionLog>> GetByEventTypeAsync(
        ExecutionEventType eventType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(el => el.EventType == eventType)
            .OrderByDescending(el => el.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExecutionLog> AddLogAsync(
        int developerStoryId,
        ExecutionEventType eventType,
        string? details = null,
        string? errorMessage = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var log = new ExecutionLog
        {
            DeveloperStoryId = developerStoryId,
            EventType = eventType,
            Details = details,
            ErrorMessage = errorMessage,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow
        };

        return await AddAsync(log, cancellationToken);
    }
}
