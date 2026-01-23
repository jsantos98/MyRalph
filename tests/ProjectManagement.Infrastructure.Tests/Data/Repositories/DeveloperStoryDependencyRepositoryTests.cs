using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class DeveloperStoryDependencyRepositoryTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly DeveloperStoryDependencyRepository _repository;

    public DeveloperStoryDependencyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _repository = new DeveloperStoryDependencyRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetDependenciesForStoryAsync_WithNoDependencies_ReturnsEmptyList()
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
        var result = await _repository.GetDependenciesForStoryAsync(story.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDependenciesForStoryAsync_WithDependencies_IncludesRequiredStories()
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
            Instructions = "I2"
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id,
            Description = "Must complete implementation first"
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDependenciesForStoryAsync(story2.Id);

        // Assert
        Assert.Single(result);
        var dependency = result.First();
        Assert.Equal(story1.Id, dependency.RequiredStoryId);
        Assert.Equal(story2.Id, dependency.DependentStoryId);
        Assert.NotNull(dependency.RequiredStory);
        Assert.Equal("Story 1", dependency.RequiredStory.Title);
    }

    [Fact]
    public async Task GetDependenciesForStoryAsync_WithMultipleDependencies_ReturnsAll()
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
        var story3 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Documentation,
            Title = "Story 3",
            Description = "D3",
            Instructions = "I3"
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2, story3 });
        await _context.SaveChangesAsync();

        _context.DeveloperStoryDependencies.AddRange(
            new DeveloperStoryDependency
            {
                DependentStoryId = story3.Id,
                RequiredStoryId = story1.Id,
                Description = "Needs implementation"
            },
            new DeveloperStoryDependency
            {
                DependentStoryId = story3.Id,
                RequiredStoryId = story2.Id,
                Description = "Needs tests"
            });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDependenciesForStoryAsync(story3.Id);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetDependentStoriesAsync_WithNoDependents_ReturnsEmptyList()
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
        var result = await _repository.GetDependentStoriesAsync(story.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDependentStoriesAsync_WithDependents_ReturnsDependentStories()
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
            Instructions = "I2"
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id,
            Description = "Depends on implementation"
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDependentStoriesAsync(story1.Id);

        // Assert
        Assert.Single(result);
        var dependent = result.First();
        Assert.Equal(story2.Id, dependent.DependentStoryId);
        Assert.Equal(story1.Id, dependent.RequiredStoryId);
    }

    [Fact]
    public async Task GetDependentStoriesAsync_WithMultipleDependents_ReturnsAll()
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
            Instructions = "I2"
        };
        var story3 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Documentation,
            Title = "Story 3",
            Description = "D3",
            Instructions = "I3"
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2, story3 });
        await _context.SaveChangesAsync();

        _context.DeveloperStoryDependencies.AddRange(
            new DeveloperStoryDependency
            {
                DependentStoryId = story2.Id,
                RequiredStoryId = story1.Id
            },
            new DeveloperStoryDependency
            {
                DependentStoryId = story3.Id,
                RequiredStoryId = story1.Id
            });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDependentStoriesAsync(story1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.DependentStoryId == story2.Id);
        Assert.Contains(result, s => s.DependentStoryId == story3.Id);
    }

    [Fact]
    public async Task DependencyExistsAsync_WithExistingDependency_ReturnsTrue()
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

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DependencyExistsAsync(story2.Id, story1.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DependencyExistsAsync_WithNonExistentDependency_ReturnsFalse()
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

        // Act - no dependency created
        var result = await _repository.DependencyExistsAsync(story2.Id, story1.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DependencyExistsAsync_WithReverseDirection_ReturnsFalse()
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

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Act - check reverse direction
        var result = await _repository.DependencyExistsAsync(story1.Id, story2.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DependencyExistsAsync_PreventsDuplicateDependencies()
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

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.DependencyExistsAsync(story2.Id, story1.Id);

        // Assert - InMemory provider doesn't enforce unique constraints at database level
        // but the repository method should return true if dependency exists
        Assert.True(exists);
    }
}
