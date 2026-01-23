using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Data.DbContext;

namespace ProjectManagement.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for DeveloperStoryDependency entity
/// </summary>
public class DeveloperStoryDependencyRepository : Repository<DeveloperStoryDependency>, IDeveloperStoryDependencyRepository
{
    public DeveloperStoryDependencyRepository(ProjectManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DeveloperStoryDependency>> GetDependenciesForStoryAsync(
        int developerStoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.RequiredStory)
            .Where(d => d.DependentStoryId == developerStoryId)
            .OrderBy(d => d.RequiredStoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeveloperStoryDependency>> GetDependentStoriesAsync(
        int developerStoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.DependentStory)
            .Where(d => d.RequiredStoryId == developerStoryId)
            .OrderBy(d => d.DependentStoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DependencyExistsAsync(
        int dependentStoryId,
        int requiredStoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(d =>
                d.DependentStoryId == dependentStoryId &&
                d.RequiredStoryId == requiredStoryId,
                cancellationToken);
    }
}
