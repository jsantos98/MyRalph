using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using Xunit;

namespace ProjectManagement.Core.Tests.Entities;

public class DeveloperStoryTests
{
    [Fact]
    public void Constructor_WithImplementationType_SetsDefaults()
    {
        // Arrange & Act
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "Test Description",
            Instructions = "Do this"
        };

        // Assert
        Assert.Equal(1, story.WorkItemId);
        Assert.Equal(DeveloperStoryType.Implementation, story.StoryType);
        Assert.Equal("Test Story", story.Title);
        Assert.Equal(DeveloperStoryStatus.Pending, story.Status);
        Assert.Equal(5, story.Priority);
        Assert.Null(story.GitBranch);
        Assert.Null(story.GitWorktree);
        Assert.Null(story.StartedAt);
        Assert.Null(story.CompletedAt);
        Assert.Null(story.ErrorMessage);
    }

    [Theory]
    [InlineData(DeveloperStoryType.Implementation, "ds-1")]
    [InlineData(DeveloperStoryType.UnitTests, "ds-2")]
    [InlineData(DeveloperStoryType.FeatureTests, "ds-3")]
    [InlineData(DeveloperStoryType.Documentation, "ds-4")]
    public void WorktreeName_ReturnsCorrectFormat(DeveloperStoryType type, string expected)
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = type,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, int.Parse(expected.Split('-')[1]));

        // Act
        var worktreeName = story.WorktreeName;

        // Assert
        Assert.Equal(expected, worktreeName);
    }

    [Fact]
    public void UpdateStatus_ToInProgress_SetsStartedAt()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Act
        story.UpdateStatus(DeveloperStoryStatus.InProgress);

        // Assert
        Assert.Equal(DeveloperStoryStatus.InProgress, story.Status);
        Assert.NotNull(story.StartedAt);
        Assert.Null(story.CompletedAt);
    }

    [Fact]
    public void UpdateStatus_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };
        story.UpdateStatus(DeveloperStoryStatus.InProgress);

        // Act
        story.UpdateStatus(DeveloperStoryStatus.Completed);

        // Assert
        Assert.Equal(DeveloperStoryStatus.Completed, story.Status);
        Assert.NotNull(story.StartedAt);
        Assert.NotNull(story.CompletedAt);
    }

    [Fact]
    public void UpdateStatus_ToPending_ResetsTimestamps()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };
        story.UpdateStatus(DeveloperStoryStatus.InProgress);
        story.UpdateStatus(DeveloperStoryStatus.Completed);

        // Act
        story.UpdateStatus(DeveloperStoryStatus.Pending);

        // Assert
        Assert.Equal(DeveloperStoryStatus.Pending, story.Status);
        Assert.Null(story.StartedAt);
        Assert.Null(story.CompletedAt);
    }

    [Fact]
    public void UpdateStatus_WithErrorMessage_SetsErrorMessage()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Act
        story.UpdateStatus(DeveloperStoryStatus.Error, "Test error");

        // Assert
        Assert.Equal(DeveloperStoryStatus.Error, story.Status);
        Assert.Equal("Test error", story.ErrorMessage);
    }

    [Theory]
    [InlineData(DeveloperStoryStatus.Pending, DeveloperStoryStatus.Ready, true)]
    [InlineData(DeveloperStoryStatus.Pending, DeveloperStoryStatus.Blocked, true)]
    [InlineData(DeveloperStoryStatus.Pending, DeveloperStoryStatus.Error, true)]
    [InlineData(DeveloperStoryStatus.Ready, DeveloperStoryStatus.InProgress, true)]
    [InlineData(DeveloperStoryStatus.Ready, DeveloperStoryStatus.Blocked, true)]
    [InlineData(DeveloperStoryStatus.Ready, DeveloperStoryStatus.Error, true)]
    [InlineData(DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Completed, true)]
    [InlineData(DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Error, true)]
    [InlineData(DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Blocked, true)]
    [InlineData(DeveloperStoryStatus.Blocked, DeveloperStoryStatus.Ready, true)]
    [InlineData(DeveloperStoryStatus.Blocked, DeveloperStoryStatus.Error, true)]
    [InlineData(DeveloperStoryStatus.Error, DeveloperStoryStatus.Pending, true)]
    [InlineData(DeveloperStoryStatus.Error, DeveloperStoryStatus.Ready, true)]
    [InlineData(DeveloperStoryStatus.Pending, DeveloperStoryStatus.Completed, false)]
    [InlineData(DeveloperStoryStatus.Completed, DeveloperStoryStatus.Pending, false)]
    public void CanTransitionTo_WithVariousStates_ReturnsExpectedResult(
        DeveloperStoryStatus current, DeveloperStoryStatus target, bool expected)
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };
        story.UpdateStatus(current);

        // Act
        var result = story.CanTransitionTo(target);

        // Assert
        Assert.Equal(expected, result);
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
            Description = "Description",
            Instructions = "Instructions"
        };

        // Act
        var result = story.AreDependenciesCompleted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreDependenciesCompleted_WithAllCompletedDependencies_ReturnsTrue()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        var requiredStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Required",
            Description = "Description",
            Instructions = "Instructions"
        };
        requiredStory.UpdateStatus(DeveloperStoryStatus.Completed);

        story.Dependencies.Add(new DeveloperStoryDependency
        {
            DependentStoryId = 1,
            RequiredStoryId = 2,
            RequiredStory = requiredStory
        });

        // Act
        var result = story.AreDependenciesCompleted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreDependenciesCompleted_WithIncompleteDependency_ReturnsFalse()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        var requiredStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Required",
            Description = "Description",
            Instructions = "Instructions"
        };
        requiredStory.UpdateStatus(DeveloperStoryStatus.Pending);

        story.Dependencies.Add(new DeveloperStoryDependency
        {
            DependentStoryId = 1,
            RequiredStoryId = 2,
            RequiredStory = requiredStory
        });

        // Act
        var result = story.AreDependenciesCompleted();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Dependencies_InitializesAsEmptyList()
    {
        // Arrange & Act
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Assert
        Assert.NotNull(story.Dependencies);
        Assert.Empty(story.Dependencies);
    }

    [Fact]
    public void DependentStories_InitializesAsEmptyList()
    {
        // Arrange & Act
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Assert
        Assert.NotNull(story.DependentStories);
        Assert.Empty(story.DependentStories);
    }

    [Fact]
    public void ExecutionLogs_InitializesAsEmptyList()
    {
        // Arrange & Act
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "Description",
            Instructions = "Instructions"
        };

        // Assert
        Assert.NotNull(story.ExecutionLogs);
        Assert.Empty(story.ExecutionLogs);
    }
}
