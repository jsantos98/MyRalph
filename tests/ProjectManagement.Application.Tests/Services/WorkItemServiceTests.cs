using Moq;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using Xunit;

namespace ProjectManagement.Application.Tests.Services;

public class WorkItemServiceTests
{
    private readonly Mock<IWorkItemRepository> _mockWorkItemRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly WorkItemService _service;

    public WorkItemServiceTests()
    {
        _mockWorkItemRepo = new Mock<IWorkItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockStateManager = new Mock<IStateManager>();
        _service = new WorkItemService(
            _mockWorkItemRepo.Object,
            _mockUnitOfWork.Object,
            _mockStateManager.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WorkItemService>>());
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesWorkItem()
    {
        // Arrange
        WorkItem? capturedWorkItem = null;
        _mockWorkItemRepo
            .Setup(r => r.AddAsync(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()))
            .Callback<WorkItem, CancellationToken>((wi, _) => capturedWorkItem = wi)
            .ReturnsAsync((WorkItem wi, CancellationToken _) => wi);
        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateAsync(
            WorkItemType.UserStory,
            "Test Story",
            "Test Description",
            "Acceptance Criteria",
            3);

        // Assert
        Assert.NotNull(capturedWorkItem);
        Assert.Equal(WorkItemType.UserStory, capturedWorkItem.Type);
        Assert.Equal("Test Story", capturedWorkItem.Title);
        Assert.Equal("Test Description", capturedWorkItem.Description);
        Assert.Equal("Acceptance Criteria", capturedWorkItem.AcceptanceCriteria);
        Assert.Equal(3, capturedWorkItem.Priority);
        Assert.Equal(WorkItemStatus.Pending, capturedWorkItem.Status);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public async Task CreateAsync_WithInvalidPriority_ThrowsArgumentException(int invalidPriority)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(
                WorkItemType.UserStory,
                "Test",
                "Description",
                null,
                invalidPriority));
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsWorkItem()
    {
        // Arrange
        var expectedWorkItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        expectedWorkItem.GetType().GetProperty("Id")?.SetValue(expectedWorkItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWorkItem);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidTransition_UpdatesStatus()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description",
            Status = WorkItemStatus.Pending
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Pending, WorkItemStatus.Refining))
            .Returns(true);

        // Act
        await _service.UpdateStatusAsync(1, WorkItemStatus.Refining);

        // Assert
        Assert.Equal(WorkItemStatus.Refining, workItem.Status);
        _mockWorkItemRepo.Verify(r => r.UpdateAsync(workItem, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidTransition_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description",
            Status = WorkItemStatus.Pending
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Pending, WorkItemStatus.Completed))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            _service.UpdateStatusAsync(1, WorkItemStatus.Completed));
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNonExistentWorkItem_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.UpdateStatusAsync(999, WorkItemStatus.Refining));
    }

    [Fact]
    public async Task HasInProgressUserStoryAsync_WithInProgressUserStory_ReturnsTrue()
    {
        // Arrange
        var inProgressWorkItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "In Progress",
            Description = "Description",
            Status = WorkItemStatus.InProgress
        };

        _mockWorkItemRepo
            .Setup(r => r.GetInProgressAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(inProgressWorkItem);

        // Act
        var result = await _service.HasInProgressUserStoryAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasInProgressUserStoryAsync_WithNoInProgressStory_ReturnsFalse()
    {
        // Arrange
        _mockWorkItemRepo
            .Setup(r => r.GetInProgressAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        // Act
        var result = await _service.HasInProgressUserStoryAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasInProgressUserStoryAsync_WithInProgressBug_ReturnsFalse()
    {
        // Arrange
        var inProgressBug = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "Bug",
            Description = "Description",
            Status = WorkItemStatus.InProgress
        };

        _mockWorkItemRepo
            .Setup(r => r.GetInProgressAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(inProgressBug);

        // Act
        var result = await _service.HasInProgressUserStoryAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllWorkItems()
    {
        // Arrange
        var workItems = new List<WorkItem>
        {
            new() { Type = WorkItemType.UserStory, Title = "WI1", Description = "D1" },
            new() { Type = WorkItemType.Bug, Title = "B1", Description = "D2" }
        };

        _mockWorkItemRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItems);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }
}
