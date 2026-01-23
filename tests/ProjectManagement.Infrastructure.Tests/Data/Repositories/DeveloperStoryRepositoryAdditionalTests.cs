using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class DeveloperStoryRepositoryAdditionalTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly DeveloperStoryRepository _repository;

    public DeveloperStoryRepositoryAdditionalTests()
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
    public async Task GetByWorkItemIdAsync_WithNoStories_ReturnsEmptyList()
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

        // Act
        var result = await _repository.GetByWorkItemIdAsync(workItem.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByWorkItemIdAsync_WithStories_ReturnsStoriesOrdered()
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
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 1",
            Description = "D1",
            Instructions = "I1",
            Priority = 3
        };
        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.FeatureTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2",
            Priority = 1
        };
        var story3 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story 3",
            Description = "D3",
            Instructions = "I3",
            Priority = 2
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2, story3 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByWorkItemIdAsync(workItem.Id);

        // Assert
        Assert.Equal(3, result.Count());
        // Should be ordered by StoryType (Implementation=0, UnitTests=1, FeatureTests=2), then Id
        Assert.Equal(DeveloperStoryType.Implementation, result.First().StoryType);
        Assert.Equal(DeveloperStoryType.FeatureTests, result.Last().StoryType);
    }

    [Fact]
    public async Task GetByWorkItemIdAsync_WithNonExistentWorkItem_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByWorkItemIdAsync(999);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReadyWithResolvedDependenciesAsync_WithNoStories_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetReadyWithResolvedDependenciesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReadyWithResolvedDependenciesAsync_WithNoReadyStories_ReturnsEmptyList()
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
        var result = await _repository.GetReadyWithResolvedDependenciesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReadyWithResolvedDependenciesAsync_WithUnresolvedDependencies_ReturnsEmptyList()
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
            Status = DeveloperStoryStatus.Pending
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
        var result = await _repository.GetReadyWithResolvedDependenciesAsync();

        // Assert
        // Story2 is Ready but depends on Story1 which is not Completed
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByStatusAsync_WithMultipleStories_ReturnsCorrectOrdering()
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
            Title = "Low Priority",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 5
        };
        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "High Priority",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(DeveloperStoryStatus.Ready);

        // Assert
        Assert.Equal(2, result.Count());
        // Should be ordered by Priority (ascending), then Id
        Assert.Equal(1, result.First().Priority);
        Assert.Equal(5, result.Last().Priority);
    }
}
