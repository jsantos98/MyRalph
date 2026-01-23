using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly Repository<WorkItem> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _repository = new Repository<WorkItem>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsEntity()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };

        // Act
        var result = await _repository.AddAsync(workItem);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D1" },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var workItem = new WorkItem { Type = WorkItemType.UserStory, Title = "Test", Description = "D" };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();
        var id = workItem.Id;

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        // Arrange
        var workItem = new WorkItem { Type = WorkItemType.UserStory, Title = "Original", Description = "D" };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        workItem.Title = "Updated";
        await _repository.UpdateAsync(workItem);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.WorkItems.FindAsync(workItem.Id);
        Assert.Equal("Updated", updated!.Title);
    }

    [Fact]
    public async Task DeleteAsync_DeletesEntity()
    {
        // Arrange
        var workItem = new WorkItem { Type = WorkItemType.UserStory, Title = "Test", Description = "D" };
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
    public async Task AnyAsync_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        _context.WorkItems.Add(new WorkItem { Type = WorkItemType.UserStory, Title = "Test", Description = "D", Status = WorkItemStatus.Pending });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(wi => wi.Status == WorkItemStatus.Pending);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_WithNoMatchingEntity_ReturnsFalse()
    {
        // Arrange
        _context.WorkItems.Add(new WorkItem { Type = WorkItemType.UserStory, Title = "Test", Description = "D", Status = WorkItemStatus.Completed });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(wi => wi.Status == WorkItemStatus.Pending);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D", Status = WorkItemStatus.Pending },
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI2", Description = "D", Status = WorkItemStatus.Pending },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D", Status = WorkItemStatus.Completed }
        );
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountAsync(wi => wi.Status == WorkItemStatus.Pending);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "User Story 1", Description = "D", Priority = 1 },
            new WorkItem { Type = WorkItemType.UserStory, Title = "User Story 2", Description = "D", Priority = 3 },
            new WorkItem { Type = WorkItemType.Bug, Title = "Bug 1", Description = "D", Priority = 1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(wi => wi.Priority == 1);

        // Assert
        Assert.Equal(2, result.Count());
    }
}
