using Moq;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Enums;
using Xunit;

namespace ProjectManagement.Application.Tests.Services;

public class StateManagerTests
{
    private readonly StateManager _stateManager;

    public StateManagerTests()
    {
        _stateManager = new StateManager();
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
    public void CanTransition_WorkItemStatus_ReturnsExpected(
        WorkItemStatus current, WorkItemStatus target, bool expected)
    {
        // Act
        var result = _stateManager.CanTransition(current, target);

        // Assert
        Assert.Equal(expected, result);
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
    public void CanTransition_DeveloperStoryStatus_ReturnsExpected(
        DeveloperStoryStatus current, DeveloperStoryStatus target, bool expected)
    {
        // Act
        var result = _stateManager.CanTransition(current, target);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetValidTransitions_WorkItemPending_ReturnsValidNextStates()
    {
        // Act
        var transitions = _stateManager.GetValidTransitions(WorkItemStatus.Pending);

        // Assert
        Assert.Contains(WorkItemStatus.Refining, transitions);
        Assert.Contains(WorkItemStatus.Error, transitions);
        Assert.DoesNotContain(WorkItemStatus.Completed, transitions);
    }

    [Fact]
    public void GetValidTransitions_DeveloperStoryPending_ReturnsValidNextStates()
    {
        // Act
        var transitions = _stateManager.GetValidTransitions(DeveloperStoryStatus.Pending);

        // Assert
        Assert.Contains(DeveloperStoryStatus.Ready, transitions);
        Assert.Contains(DeveloperStoryStatus.Blocked, transitions);
        Assert.Contains(DeveloperStoryStatus.Error, transitions);
        Assert.DoesNotContain(DeveloperStoryStatus.Completed, transitions);
    }

    [Fact]
    public void GetValidTransitions_WorkItemInProgress_ReturnsOnlyCompletedOrError()
    {
        // Act
        var transitions = _stateManager.GetValidTransitions(WorkItemStatus.InProgress);

        // Assert
        Assert.Contains(WorkItemStatus.Completed, transitions);
        Assert.Contains(WorkItemStatus.Error, transitions);
        Assert.Equal(2, transitions.Count());
    }

    [Fact]
    public void GetValidTransitions_DeveloperStoryCompleted_ReturnsNoTransitions()
    {
        // Act
        var transitions = _stateManager.GetValidTransitions(DeveloperStoryStatus.Completed);

        // Assert
        Assert.Empty(transitions);
    }
}
