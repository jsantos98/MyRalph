using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class ExecutionLogRepositoryAdditionalTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly ExecutionLogRepository _repository;

    public ExecutionLogRepositoryAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _repository = new ExecutionLogRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByDeveloperStoryIdAsync_WithNoLogs_ReturnsEmptyList()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDeveloperStoryIdAsync(story.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByDeveloperStoryIdAsync_WithMultipleEvents_ReturnsAllEvents()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Add multiple logs - save after each
        await _repository.AddLogAsync(story.Id, ExecutionEventType.Started, "Started");
        await _context.SaveChangesAsync();
        await Task.Delay(10);
        await _repository.AddLogAsync(story.Id, ExecutionEventType.Info, "Processing");
        await _context.SaveChangesAsync();
        await Task.Delay(10);
        await _repository.AddLogAsync(story.Id, ExecutionEventType.Completed, "Done");
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDeveloperStoryIdAsync(story.Id);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetByDeveloperStoryIdAsync_WithNonExistentStory_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByDeveloperStoryIdAsync(999);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEventTypeAsync_WithMultipleLogsOfSameType_ReturnsAllMatching()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
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

        await _repository.AddLogAsync(story1.Id, ExecutionEventType.Started, "Started 1");
        await _context.SaveChangesAsync();
        await _repository.AddLogAsync(story2.Id, ExecutionEventType.Started, "Started 2");
        await _context.SaveChangesAsync();
        await _repository.AddLogAsync(story1.Id, ExecutionEventType.Completed, "Done");
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEventTypeAsync(ExecutionEventType.Started);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, log => Assert.Equal(ExecutionEventType.Started, log.EventType));
    }

    [Fact]
    public async Task GetByEventTypeAsync_WithNoMatchingLogs_ReturnsEmptyList()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        await _repository.AddLogAsync(story.Id, ExecutionEventType.Started, "Started");

        // Act
        var result = await _repository.GetByEventTypeAsync(ExecutionEventType.Completed);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddLogAsync_WithAllParameters_SavesCorrectly()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AddLogAsync(
            story.Id,
            ExecutionEventType.Failed,
            "Error details",
            "Error message");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(story.Id, result.DeveloperStoryId);
        Assert.Equal(ExecutionEventType.Failed, result.EventType);
        Assert.Equal("Error details", result.Details);
        Assert.Equal("Error message", result.ErrorMessage);
    }

    [Fact]
    public async Task AddLogAsync_WithNullErrorMessage_SavesCorrectly()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AddLogAsync(
            story.Id,
            ExecutionEventType.Started,
            "Started",
            null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }
}
