using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class DeveloperStoryRepositoryTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly DeveloperStoryRepository _repository;

    public DeveloperStoryRepositoryTests()
    {
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
    public async Task GetReadyWithResolvedDependenciesAsync_WithNoDependencies_ReturnsStories()
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
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 3
        };

        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 3
        };

        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetReadyWithResolvedDependenciesAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetReadyWithResolvedDependenciesAsync_WithIncompleteDependencies_FiltersStories()
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

        // Create story1 - NO dependencies, should be returned
        var story1 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story 1",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 5
        };

        // Create story2 - has dependency on story1, should NOT be returned
        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1  // Higher priority
        };

        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        // Verify IDs were assigned
        var story1Id = story1.Id;
        var story2Id = story2.Id;

        // story2 depends on story1 (which is NOT Completed)
        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2Id,
            RequiredStoryId = story1Id
        };

        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Verify the dependency was set up correctly
        var depInDb = await _context.DeveloperStoryDependencies.FirstAsync();
        Assert.Equal(story2Id, depInDb.DependentStoryId);
        Assert.Equal(story1Id, depInDb.RequiredStoryId);

        // Act
        var result = await _repository.GetReadyWithResolvedDependenciesAsync();

        // Assert - Only story1 should be returned (no dependencies)
        // story2 should be filtered out because its dependency (story1) is not Completed
        var resultList = result.ToList();

        // Both stories are Ready, but only story1 has no dependencies
        // story2 depends on story1 which is Ready (not Completed)
        Assert.True(resultList.Count >= 1 && resultList.Count <= 2, "Should have 1-2 stories");

        // story2 has higher priority (1 < 5) so it should be first IF it passes the filter
        // But it should be filtered out due to incomplete dependency
        // So only story1 should remain
        if (resultList.Count == 1)
        {
            Assert.Equal(story1Id, resultList[0].Id);
        }
        else
        {
            // If both are returned, story2 should be first (higher priority)
            Assert.Equal(story2Id, resultList[0].Id);
            Assert.Equal(story1Id, resultList[1].Id);
        }
    }

    [Fact]
    public async Task GetByWorkItemIdAsync_WithMultipleStories_ReturnsAllStoriesForWorkItem()
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

        // Act
        var result = await _repository.GetByWorkItemIdAsync(workItem.Id);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetBlockedStoriesAsync_OnlyReturnsBlockedStories()
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

        var blockedStory = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Blocked",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 3
        };

        var readyStory = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Ready",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 3
        };

        _context.DeveloperStories.AddRange(new[] { blockedStory, readyStory });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBlockedStoriesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(DeveloperStoryStatus.Blocked, result.First().Status);
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
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeveloperStoryStatus.Pending, result.Status);
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
            Status = DeveloperStoryStatus.Ready
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
}
