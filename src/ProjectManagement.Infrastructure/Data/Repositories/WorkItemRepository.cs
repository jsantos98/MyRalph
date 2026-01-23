using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Data.DbContext;

namespace ProjectManagement.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for WorkItem entity
/// </summary>
public class WorkItemRepository : Repository<WorkItem>, IWorkItemRepository
{
    public WorkItemRepository(ProjectManagementDbContext context) : base(context)
    {
    }

    public async Task<WorkItem?> GetWithDeveloperStoriesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(wi => wi.DeveloperStories)
            .FirstOrDefaultAsync(wi => wi.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetByStatusAsync(
        WorkItemStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(wi => wi.Status == status)
            .OrderBy(wi => wi.Priority)
            .ThenBy(wi => wi.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetByTypeAsync(
        WorkItemType type,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(wi => wi.Type == type)
            .OrderByDescending(wi => wi.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkItem?> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(wi =>
                wi.Status == WorkItemStatus.InProgress &&
                wi.Type == WorkItemType.UserStory,
                cancellationToken);
    }
}
