using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class RepositoryBaseTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly WorkItemRepository _repository;

    public RepositoryBaseTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _repository = new WorkItemRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(workItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentEntity_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleEntities_ReturnsAll()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D1", Priority = 3 },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D2", Priority = 3 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithNoEntities_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_SetsTimestamp()
    {
        // Arrange
        var beforeAdd = DateTime.UtcNow;
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };

        // Act
        await _repository.AddAsync(workItem);

        // Assert
        Assert.True(workItem.CreatedAt >= beforeAdd);
        Assert.True(workItem.UpdatedAt >= beforeAdd);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEntity_UpdatesEntity()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Original",
            Description = "Original"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        workItem.Title = "Updated";
        workItem.Description = "Updated";

        // Act
        await _repository.UpdateAsync(workItem);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.WorkItems.FindAsync(workItem.Id);
        Assert.Equal("Updated", updated!.Title);
        Assert.Equal("Updated", updated.Description);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_RemovesEntity()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(workItem);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.WorkItems.FindAsync(workItem.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task AnyAsync_WithExistingEntity_ReturnsTrue()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(wi => wi.Id == workItem.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_WithNonExistentEntity_ReturnsFalse()
    {
        // Act
        var result = await _repository.AnyAsync(wi => wi.Id == 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task FindAsync_WithMultipleEntities_ReturnsFilteredResults()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _context.WorkItems.Add(new WorkItem
            {
                Type = WorkItemType.UserStory,
                Title = $"Item {i}",
                Description = $"Description {i}",
                Priority = i
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var highPriority = await _repository.FindAsync(wi => wi.Priority <= 5);
        var mediumPriority = await _repository.FindAsync(wi => wi.Priority > 5 && wi.Priority <= 15);

        // Assert
        Assert.Equal(5, highPriority.Count());
        Assert.Equal(10, mediumPriority.Count());
    }

    [Fact]
    public async Task FindAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            _context.WorkItems.Add(new WorkItem
            {
                Type = WorkItemType.UserStory,
                Title = $"Item {i}",
                Description = $"Description {i}",
                Priority = i
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(wi => wi.Priority > 100);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D", Priority = 1 },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D", Priority = 2 },
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI2", Description = "D", Priority = 5 }
        );
        await _context.SaveChangesAsync();

        // Act
        var highPriority = await _repository.CountAsync(wi => wi.Priority <= 2);
        var allCount = await _repository.CountAsync(wi => true);

        // Assert
        Assert.Equal(2, highPriority);
        Assert.Equal(3, allCount);
    }

    [Fact]
    public async Task CountAsync_WithNoEntities_ReturnsZero()
    {
        // Act
        var result = await _repository.CountAsync(wi => true);

        // Assert
        Assert.Equal(0, result);
    }
}
