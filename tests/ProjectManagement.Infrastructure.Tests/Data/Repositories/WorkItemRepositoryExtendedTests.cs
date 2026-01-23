using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class WorkItemRepositoryExtendedTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly WorkItemRepository _repository;

    public WorkItemRepositoryExtendedTests()
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
    public async Task GetWithDeveloperStoriesAsync_WithStories_IncludesDeveloperStories()
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
            Instructions = "I1"
        };

        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2"
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
    public async Task GetWithDeveloperStoriesAsync_WithNonExistentWorkItem_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWithDeveloperStoriesAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByStatusAsync_WithMultipleItems_ReturnsFilteredItems()
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
    public async Task GetByStatusAsync_WithNoMatchingItems_ReturnsEmptyList()
    {
        // Arrange
        _context.WorkItems.Add(new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D",
            Status = WorkItemStatus.InProgress
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(WorkItemStatus.Pending);

        // Assert
        Assert.Empty(result);
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
    public async Task GetByTypeAsync_WithBugs_ReturnsOnlyBugs()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D", Priority = 3 },
            new WorkItem { Type = WorkItemType.Bug, Title = "B1", Description = "D", Priority = 3 },
            new WorkItem { Type = WorkItemType.Bug, Title = "B2", Description = "D", Priority = 3 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(WorkItemType.Bug);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, wi => Assert.Equal(WorkItemType.Bug, wi.Type));
    }

    [Fact]
    public async Task GetByTypeAsync_WithNoMatchingItems_ReturnsEmptyList()
    {
        // Arrange
        _context.WorkItems.Add(new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(WorkItemType.Bug);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInProgressAsync_WithInProgressUserStory_ReturnsUserStory()
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
        Assert.Equal(WorkItemType.UserStory, result.Type);
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

        // Assert - business rule: only UserStories can be InProgress
        Assert.Null(result);
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
    public async Task GetInProgressAsync_WithInProgressUserStoryAndBug_ReturnsUserStory()
    {
        // Arrange
        _context.WorkItems.AddRange(
            new WorkItem
            {
                Type = WorkItemType.UserStory,
                Title = "In Progress US",
                Description = "D",
                Status = WorkItemStatus.InProgress
            },
            new WorkItem
            {
                Type = WorkItemType.Bug,
                Title = "In Progress Bug",
                Description = "D",
                Status = WorkItemStatus.InProgress
            });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetInProgressAsync();

        // Assert - should return UserStory, not Bug
        Assert.NotNull(result);
        Assert.Equal(WorkItemType.UserStory, result.Type);
        Assert.Equal("In Progress US", result.Title);
    }
}
