using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class ExecutionLogRepositoryTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly ExecutionLogRepository _repository;

    public ExecutionLogRepositoryTests()
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
        // Arrange - no logs added

        // Act
        var result = await _repository.GetByDeveloperStoryIdAsync(1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByDeveloperStoryIdAsync_WithMultipleLogs_ReturnsLogsOrderedByTimestamp()
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

        var log1 = new ExecutionLog
        {
            DeveloperStoryId = story.Id,
            EventType = ExecutionEventType.Started,
            Details = "First"
        };
        _context.ExecutionLogs.Add(log1);
        await _context.SaveChangesAsync();

        // Add delay to ensure different timestamps
        await Task.Delay(10);

        var log2 = new ExecutionLog
        {
            DeveloperStoryId = story.Id,
            EventType = ExecutionEventType.Completed,
            Details = "Second"
        };
        _context.ExecutionLogs.Add(log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDeveloperStoryIdAsync(story.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, l => l.EventType == ExecutionEventType.Started);
        Assert.Contains(result, l => l.EventType == ExecutionEventType.Completed);
    }

    [Fact]
    public async Task GetByDeveloperStoryIdAsync_WithLogsForOtherStories_ReturnsOnlyMatchingLogs()
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
            Description = "D",
            Instructions = "I"
        };
        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        var log1 = new ExecutionLog
        {
            DeveloperStoryId = story1.Id,
            EventType = ExecutionEventType.Started,
            Details = "Story 1 log"
        };
        var log2 = new ExecutionLog
        {
            DeveloperStoryId = story2.Id,
            EventType = ExecutionEventType.Started,
            Details = "Story 2 log"
        };
        _context.ExecutionLogs.AddRange(new[] { log1, log2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDeveloperStoryIdAsync(story1.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Story 1 log", result.First().Details);
    }

    [Fact]
    public async Task GetByEventTypeAsync_WithMatchingEvents_ReturnsFilteredLogs()
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

        _context.ExecutionLogs.AddRange(
            new ExecutionLog
            {
                DeveloperStoryId = story.Id,
                EventType = ExecutionEventType.Started,
                Details = "Started"
            },
            new ExecutionLog
            {
                DeveloperStoryId = story.Id,
                EventType = ExecutionEventType.Completed,
                Details = "Completed"
            },
            new ExecutionLog
            {
                DeveloperStoryId = story.Id,
                EventType = ExecutionEventType.Started,
                Details = "Started again"
            });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEventTypeAsync(ExecutionEventType.Started);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, log => Assert.Equal(ExecutionEventType.Started, log.EventType));
    }

    [Fact]
    public async Task GetByEventTypeAsync_WithNoMatchingEvents_ReturnsEmptyList()
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

        _context.ExecutionLogs.Add(new ExecutionLog
        {
            DeveloperStoryId = story.Id,
            EventType = ExecutionEventType.Started,
            Details = "Started"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEventTypeAsync(ExecutionEventType.Failed);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddLogAsync_WithValidData_SetsDefaults()
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

        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await _repository.AddLogAsync(
            story.Id,
            ExecutionEventType.Started,
            "Test details");

        // Assert
        Assert.True(result.Timestamp >= beforeAdd);
    }

    [Fact]
    public async Task AddLogAsync_WithErrorMessage_SavesErrorMessage()
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
            "Test details",
            "Something went wrong");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(story.Id, result.DeveloperStoryId);
        Assert.Equal(ExecutionEventType.Failed, result.EventType);
        Assert.Equal("Something went wrong", result.ErrorMessage);
    }

    [Fact]
    public async Task AddLogAsync_WithMetadata_SavesMetadata()
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

        var metadata = @"{""duration"": ""00:05:00"", ""exitCode"": 0}";

        // Act
        var result = await _repository.AddLogAsync(
            story.Id,
            ExecutionEventType.Completed,
            "Test details",
            metadata: metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metadata, result.Metadata);
    }
}
