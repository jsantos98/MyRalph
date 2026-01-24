using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Data.DbContext;

namespace ProjectManagement.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for DeveloperStory entity
/// </summary>
public class DeveloperStoryRepository : Repository<DeveloperStory>, IDeveloperStoryRepository
{
    public DeveloperStoryRepository(ProjectManagementDbContext context) : base(context)
    {
    }

    public async Task<DeveloperStory?> GetWithDependenciesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ds => ds.Dependencies)
            .Include(ds => ds.DependentStories)
            .FirstOrDefaultAsync(ds => ds.Id == id, cancellationToken);
    }

    public async Task<DeveloperStory?> GetWithExecutionLogsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ds => ds.ExecutionLogs)
            .FirstOrDefaultAsync(ds => ds.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<DeveloperStory>> GetByWorkItemIdAsync(
        int workItemId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ds => ds.WorkItemId == workItemId)
            .OrderBy(ds => ds.StoryType)
            .ThenBy(ds => ds.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeveloperStory>> GetByStatusAsync(
        DeveloperStoryStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ds => ds.Status == status)
            .OrderBy(ds => ds.Priority)
            .ThenBy(ds => ds.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeveloperStory>> GetReadyWithResolvedDependenciesAsync(
        CancellationToken cancellationToken = default)
    {
        // Get all Ready stories with their dependencies (stories they depend on)
        var readyStories = await _dbSet
            .Include(ds => ds.Dependencies)  // Stories this story depends on (blocking dependencies)
            .Where(ds => ds.Status == DeveloperStoryStatus.Ready)
            .OrderBy(ds => ds.Priority)
            .ThenBy(ds => ds.Id)
            .ToListAsync(cancellationToken);

        // Get the IDs of all required stories (stories that need to be completed first)
        var requiredStoryIds = readyStories
            .SelectMany(ds => ds.Dependencies)
            .Select(d => d.RequiredStoryId)
            .Distinct()
            .ToList();

        // Load the required stories explicitly
        var requiredStories = await _dbSet
            .Where(s => requiredStoryIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        // Filter to only include stories where all dependencies are completed
        return readyStories
            .Where(ds =>
            {
                if (!ds.Dependencies.Any())
                    return true; // No dependencies, can be executed

                // Check if all required stories are completed
                return ds.Dependencies.All(d =>
                    requiredStories.TryGetValue(d.RequiredStoryId, out var requiredStory) &&
                    requiredStory.Status == DeveloperStoryStatus.Completed);
            })
            .ToList();
    }

    public async Task<IEnumerable<DeveloperStory>> GetBlockedStoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ds => ds.Status == DeveloperStoryStatus.Blocked)
            .OrderBy(ds => ds.Priority)
            .ThenBy(ds => ds.Id)
            .ToListAsync(cancellationToken);
    }
}
