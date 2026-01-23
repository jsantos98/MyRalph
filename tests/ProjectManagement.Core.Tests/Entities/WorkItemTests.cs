using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using Xunit;

namespace ProjectManagement.Core.Tests.Entities;

public class WorkItemTests
{
    [Fact]
    public void Constructor_WithUserStoryType_SetsDefaults()
    {
        // Arrange & Act
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description"
        };

        // Assert
        Assert.Equal(WorkItemType.UserStory, workItem.Type);
        Assert.Equal("Test Story", workItem.Title);
        Assert.Equal("Test Description", workItem.Description);
        Assert.Equal(WorkItemStatus.Pending, workItem.Status);
        Assert.Equal(5, workItem.Priority);
        Assert.Null(workItem.AcceptanceCriteria);
        Assert.Null(workItem.ErrorMessage);
        Assert.True(workItem.CreatedAt <= DateTime.UtcNow);
        Assert.True(workItem.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithBugType_SetsDefaults()
    {
        // Arrange & Act
        var workItem = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "Test Bug",
            Description = "Bug Description"
        };

        // Assert
        Assert.Equal(WorkItemType.Bug, workItem.Type);
        Assert.Equal(WorkItemStatus.Pending, workItem.Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public void Constructor_WithValidPriority_SetsPriority(int priority)
    {
        // Arrange & Act
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description",
            Priority = priority
        };

        // Assert
        Assert.Equal(priority, workItem.Priority);
    }

    [Fact]
    public void UpdateStatus_WithValidTransition_UpdatesStatusAndTimestamp()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        var originalTimestamp = workItem.UpdatedAt;

        // Act - add a small delay to ensure timestamp difference
        Thread.Sleep(10);
        workItem.UpdateStatus(WorkItemStatus.Refining);

        // Assert
        Assert.Equal(WorkItemStatus.Refining, workItem.Status);
        Assert.True(workItem.UpdatedAt > originalTimestamp);
    }

    [Fact]
    public void UpdateStatus_WithErrorMessage_SetsErrorMessage()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };

        // Act
        workItem.UpdateStatus(WorkItemStatus.Error, "Something went wrong");

        // Assert
        Assert.Equal(WorkItemStatus.Error, workItem.Status);
        Assert.Equal("Something went wrong", workItem.ErrorMessage);
    }

    [Theory]
    [InlineData(WorkItemStatus.Pending, WorkItemStatus.Refining, true)]
    [InlineData(WorkItemStatus.Pending, WorkItemStatus.Error, true)]
    [InlineData(WorkItemStatus.Refining, WorkItemStatus.Refined, true)]
    [InlineData(WorkItemStatus.Refining, WorkItemStatus.Error, true)]
    [InlineData(WorkItemStatus.Refined, WorkItemStatus.InProgress, true)]
    [InlineData(WorkItemStatus.Refined, WorkItemStatus.Error, true)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Completed, true)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Error, true)]
    [InlineData(WorkItemStatus.Error, WorkItemStatus.Pending, true)]
    [InlineData(WorkItemStatus.Error, WorkItemStatus.Refining, true)]
    [InlineData(WorkItemStatus.Pending, WorkItemStatus.Completed, false)]
    [InlineData(WorkItemStatus.Refining, WorkItemStatus.Pending, false)]
    [InlineData(WorkItemStatus.Completed, WorkItemStatus.Pending, false)]
    public void CanTransitionTo_WithVariousStates_ReturnsExpectedResult(
        WorkItemStatus current, WorkItemStatus target, bool expected)
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        workItem.UpdateStatus(current);

        // Act
        var result = workItem.CanTransitionTo(target);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BranchPrefix_WithUserStory_ReturnsUs()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };

        // Act
        var prefix = workItem.BranchPrefix;

        // Assert
        Assert.Equal("us", prefix);
    }

    [Fact]
    public void BranchPrefix_WithBug_ReturnsBug()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "Test",
            Description = "Description"
        };

        // Act
        var prefix = workItem.BranchPrefix;

        // Assert
        Assert.Equal("bug", prefix);
    }

    [Fact]
    public void DefaultBranchName_WithUserStory_ReturnsUsId()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        workItem.UpdateStatus(WorkItemStatus.Refined);
        // Simulate having an ID (normally set by database)
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 123);

        // Act
        var branchName = workItem.DefaultBranchName;

        // Assert
        Assert.Equal("us-123", branchName);
    }

    [Fact]
    public void DeveloperStories_InitializesAsEmptyList()
    {
        // Arrange & Act
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };

        // Assert
        Assert.NotNull(workItem.DeveloperStories);
        Assert.Empty(workItem.DeveloperStories);
    }
}
