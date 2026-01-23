using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class WorkItemRepositoryTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly WorkItemRepository _repository;

    public WorkItemRepositoryTests()
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
    public async Task GetWithDeveloperStoriesAsync_WithNoStories_ReturnsWorkItemWithEmptyStories()
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
        var result = await _repository.GetWithDeveloperStoriesAsync(workItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.DeveloperStories);
    }

    [Fact]
    public async Task GetWithDeveloperStoriesAsync_WithStories_ReturnsWorkItemWithStories()
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

        var story1 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story 1",
            Description = "D1",
            Instructions = "I1",
            Status = DeveloperStoryStatus.Ready
        };

        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2",
            Status = DeveloperStoryStatus.Pending
        };

        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithDeveloperStoriesAsync(workItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.DeveloperStories.Count);
    }

    [Fact]
    public async Task GetByStatusAsync_WithMatchingItems_ReturnsFilteredItems()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D", Status = WorkItemStatus.Pending, Priority = 3 },
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI2", Description = "D", Status = WorkItemStatus.Completed, Priority = 3 },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D", Status = WorkItemStatus.Pending, Priority = 3 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(WorkItemStatus.Pending);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, wi => Assert.Equal(WorkItemStatus.Pending, wi.Status));
    }

    [Fact]
    public async Task GetByTypeAsync_WithUserStories_ReturnsOnlyUserStories()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D", Priority = 3 },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D", Priority = 3 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(WorkItemType.UserStory);

        // Assert
        Assert.Single(result);
        Assert.Equal(WorkItemType.UserStory, result.First().Type);
    }

    [Fact]
    public async Task GetInProgressAsync_WithInProgressItem_ReturnsInProgressItem()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "In Progress",
            Description = "D",
            Status = WorkItemStatus.InProgress
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetInProgressAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task GetInProgressAsync_WithNoInProgressItem_ReturnsNull()
    {
        // Act
        var result = await _repository.GetInProgressAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInProgressAsync_WithInProgressBug_ReturnsNull()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "In Progress Bug",
            Description = "D",
            Status = WorkItemStatus.InProgress
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetInProgressAsync();

        // Assert
        Assert.Null(result); // Business rule: only UserStories can be InProgress
    }

    [Fact]
    public async Task AddAsync_WithWorkItem_SetsCreatedAtAndUpdatedAt()
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
        await _context.SaveChangesAsync();

        // Assert
        Assert.True(workItem.CreatedAt >= beforeAdd);
        Assert.True(workItem.UpdatedAt >= beforeAdd);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesWorkItem()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Original Title",
            Description = "Original Description",
            Status = WorkItemStatus.Pending
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = workItem.UpdatedAt;

        // Act - add delay to ensure timestamp difference
        await Task.Delay(10);
        workItem.Title = "Updated Title";
        workItem.Description = "Updated Description";
        workItem.UpdateStatus(WorkItemStatus.Refining);
        await _repository.UpdateAsync(workItem);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.WorkItems.FindAsync(workItem.Id);
        Assert.Equal("Updated Title", updated!.Title);
        Assert.Equal("Updated Description", updated.Description);
        Assert.Equal(WorkItemStatus.Refining, updated.Status);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);
    }
}
