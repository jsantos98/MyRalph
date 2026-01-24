using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class DeveloperStoryRepositoryExtendedTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly DeveloperStoryRepository _repository;
    private readonly ITestOutputHelper _output;

    public DeveloperStoryRepositoryExtendedTests(ITestOutputHelper output)
    {
        _output = output;
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _repository = new DeveloperStoryRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetWithDependenciesAsync_WithNoDependencies_ReturnsStory()
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
        var result = await _repository.GetWithDependenciesAsync(story.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Dependencies);
        Assert.Empty(result.DependentStories);
    }

    [Fact]
    public async Task GetWithDependenciesAsync_WithDependencies_IncludesNavigationProperties()
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
            Instructions = "I1",
            Status = DeveloperStoryStatus.Completed
        };
        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2",
            Status = DeveloperStoryStatus.Ready
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithDependenciesAsync(story2.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Dependencies);
        Assert.Equal(story1.Id, result.Dependencies.First().RequiredStoryId);
    }

    [Fact]
    public async Task GetWithExecutionLogsAsync_WithNoLogs_ReturnsStory()
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
        var result = await _repository.GetWithExecutionLogsAsync(story.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ExecutionLogs);
    }

    [Fact]
    public async Task GetWithExecutionLogsAsync_WithLogs_IncludesLogs()
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
        var result = await _repository.GetWithExecutionLogsAsync(story.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.ExecutionLogs);
        Assert.Equal(ExecutionEventType.Started, result.ExecutionLogs.First().EventType);
    }

    [Fact]
    public async Task GetByStatusAsync_WithMultipleStories_ReturnsFilteredStories()
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

        _context.DeveloperStories.AddRange(
            new DeveloperStory
            {
                WorkItemId = workItem.Id,
                StoryType = DeveloperStoryType.Implementation,
                Title = "Story 1",
                Description = "D1",
                Instructions = "I1",
                Status = DeveloperStoryStatus.Ready,
                Priority = 3
            },
            new DeveloperStory
            {
                WorkItemId = workItem.Id,
                StoryType = DeveloperStoryType.UnitTests,
                Title = "Story 2",
                Description = "D2",
                Instructions = "I2",
                Status = DeveloperStoryStatus.Completed,
                Priority = 3
            },
            new DeveloperStory
            {
                WorkItemId = workItem.Id,
                StoryType = DeveloperStoryType.FeatureTests,
                Title = "Story 3",
                Description = "D3",
                Instructions = "I3",
                Status = DeveloperStoryStatus.Blocked,
                Priority = 3
            });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(DeveloperStoryStatus.Ready);

        // Assert
        Assert.Single(result);
        Assert.Equal(DeveloperStoryStatus.Ready, result.First().Status);
    }

    [Fact]
    public async Task GetByStatusAsync_WithNoMatchingStories_ReturnsEmptyList()
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
            Instructions = "I",
            Status = DeveloperStoryStatus.Pending
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(DeveloperStoryStatus.Ready);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBlockedStoriesAsync_WithNoBlockedStories_ReturnsEmptyList()
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
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBlockedStoriesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBlockedStoriesAsync_WithBlockedStories_ReturnsBlockedStories()
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

        var blockedStory1 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Blocked 1",
            Description = "D1",
            Instructions = "I1",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 5
        };

        var blockedStory2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Blocked 2",
            Description = "D2",
            Instructions = "I2",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 3
        };

        var readyStory = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.FeatureTests,
            Title = "Ready",
            Description = "D3",
            Instructions = "I3",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };

        _context.DeveloperStories.AddRange(new[] { blockedStory1, blockedStory2, readyStory });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBlockedStoriesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.Equal(DeveloperStoryStatus.Blocked, s.Status));
        // Should be ordered by priority
        Assert.Equal(3, result.First().Priority);
    }

    [Fact]
    public async Task AddAsync_WithDeveloperStory_SetsDefaults()
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
            Title = "Test Story",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Act
        var result = await _repository.AddAsync(story);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeveloperStoryStatus.Pending, result.Status);
        Assert.Equal(5, result.Priority);
    }

    [Fact]
    public async Task AddAsync_WithBugWorkItem_SetsDefaultPriority()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "Bug",
            Description = "D",
            Priority = 1
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Fix Story",
            Description = "D",
            Instructions = "I"
        };

        // Act
        var result = await _repository.AddAsync(story);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeveloperStoryStatus.Pending, result.Status);
        // Priority is set to default (5), not inherited from parent
        Assert.Equal(5, result.Priority);
    }

    [Fact]
    public async Task UpdateStatus_UpdatesStoryAndSetsTimestamps()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Pending
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        story.UpdateStatus(DeveloperStoryStatus.InProgress);
        await _repository.UpdateAsync(story);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.DeveloperStories.FindAsync(story.Id);
        Assert.Equal(DeveloperStoryStatus.InProgress, updated!.Status);
        Assert.NotNull(updated.StartedAt);
    }

    [Fact]
    public async Task UpdateStatus_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.InProgress
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Act
        story.UpdateStatus(DeveloperStoryStatus.Completed);
        await _repository.UpdateAsync(story);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.DeveloperStories.FindAsync(story.Id);
        Assert.Equal(DeveloperStoryStatus.Completed, updated!.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task UpdateStatus_ToError_SetsErrorMessage()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.InProgress
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        var errorMessage = "Test error message";

        // Act
        story.UpdateStatus(DeveloperStoryStatus.Error, errorMessage);
        await _repository.UpdateAsync(story);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.DeveloperStories.FindAsync(story.Id);
        Assert.Equal(DeveloperStoryStatus.Error, updated!.Status);
        Assert.Equal(errorMessage, updated.ErrorMessage);
    }

    [Fact]
    public void AreDependenciesCompleted_WithNoDependencies_ReturnsTrue()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Dependencies = new List<DeveloperStoryDependency>()
        };

        // Act
        var result = story.AreDependenciesCompleted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreDependenciesCompleted_WithIncompleteDependencies_ReturnsFalse()
    {
        // Arrange
        var requiredStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Required",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready
        };
        requiredStory.GetType().GetProperty("Id")?.SetValue(requiredStory, 1);

        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Dependent",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Dependencies = new List<DeveloperStoryDependency>
            {
                new DeveloperStoryDependency
                {
                    DependentStoryId = 2,
                    RequiredStoryId = 1,
                    RequiredStory = requiredStory
                }
            }
        };

        // Act
        var result = story.AreDependenciesCompleted();

        // Assert
        Assert.False(result);
    }
}
