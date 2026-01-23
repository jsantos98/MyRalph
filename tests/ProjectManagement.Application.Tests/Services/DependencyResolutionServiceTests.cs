using Moq;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using Xunit;

namespace ProjectManagement.Application.Tests.Services;

public class DependencyResolutionServiceTests
{
    private readonly Mock<IDeveloperStoryRepository> _mockDeveloperStoryRepo;
    private readonly Mock<IDeveloperStoryDependencyRepository> _mockDependencyRepo;
    private readonly Mock<IWorkItemRepository> _mockWorkItemRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly DependencyResolutionService _service;

    public DependencyResolutionServiceTests()
    {
        _mockDeveloperStoryRepo = new Mock<IDeveloperStoryRepository>();
        _mockDependencyRepo = new Mock<IDeveloperStoryDependencyRepository>();
        _mockWorkItemRepo = new Mock<IWorkItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new DependencyResolutionService(
            _mockDeveloperStoryRepo.Object,
            _mockDependencyRepo.Object,
            _mockWorkItemRepo.Object,
            _mockUnitOfWork.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<DependencyResolutionService>>());
    }

    [Fact]
    public async Task SelectNextAsync_WithNoReadyStories_ReturnsNull()
    {
        // Arrange
        _mockDeveloperStoryRepo
            .Setup(r => r.GetReadyWithResolvedDependenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory>());
        _mockWorkItemRepo
            .Setup(r => r.GetInProgressAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        // Act
        var result = await _service.SelectNextAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectNextAsync_WithInProgressUserStory_ReturnsNull()
    {
        // Arrange
        var inProgressWorkItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "In Progress",
            Description = "Description"
        };
        inProgressWorkItem.UpdateStatus(WorkItemStatus.InProgress);

        _mockWorkItemRepo
            .Setup(r => r.GetInProgressAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(inProgressWorkItem);

        // Act
        var result = await _service.SelectNextAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectNextAsync_WithReadyStories_ReturnsHighestPriority()
    {
        // Arrange
        var workItem1 = new WorkItem { Type = WorkItemType.UserStory, Title = "WI1", Description = "D", Priority = 3 };
        var workItem2 = new WorkItem { Type = WorkItemType.UserStory, Title = "WI2", Description = "D", Priority = 1 };
        workItem1.UpdateStatus(WorkItemStatus.Refined);
        workItem2.UpdateStatus(WorkItemStatus.Refined);
        workItem1.GetType().GetProperty("Id")?.SetValue(workItem1, 1);
        workItem2.GetType().GetProperty("Id")?.SetValue(workItem2, 2);

        var story1 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Low Priority",
            Description = "D",
            Instructions = "I",
            Priority = 3,
            Status = DeveloperStoryStatus.Ready
        };
        story1.GetType().GetProperty("Id")?.SetValue(story1, 1);

        var story2 = new DeveloperStory
        {
            WorkItemId = 2,
            StoryType = DeveloperStoryType.Implementation,
            Title = "High Priority",
            Description = "D",
            Instructions = "I",
            Priority = 1,
            Status = DeveloperStoryStatus.Ready
        };
        story2.GetType().GetProperty("Id")?.SetValue(story2, 2);

        _mockWorkItemRepo
            .Setup(r => r.GetInProgressAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);
        _mockDeveloperStoryRepo
            .Setup(r => r.GetReadyWithResolvedDependenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory> { story1, story2 });
        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => id == 1 ? workItem1 : workItem2);

        // Act
        var result = await _service.SelectNextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id); // Higher priority story (lower number)
    }

    [Fact]
    public async Task UpdateDependencyStatusesAsync_UpdatesBlockedToReady()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Blocked
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        var requiredStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Required",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Completed
        };
        requiredStory.GetType().GetProperty("Id")?.SetValue(requiredStory, 2);

        var dependency = new DeveloperStoryDependency
        {
            DependentStoryId = 1,
            RequiredStoryId = 2,
            RequiredStory = requiredStory
        };

        story.Dependencies.Add(dependency);

        _mockDeveloperStoryRepo
            .Setup(r => r.GetByStatusAsync(DeveloperStoryStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory>());
        _mockDeveloperStoryRepo
            .Setup(r => r.GetByStatusAsync(DeveloperStoryStatus.Blocked, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory> { story });
        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        // Act
        await _service.UpdateDependencyStatusesAsync();

        // Assert
        Assert.Equal(DeveloperStoryStatus.Ready, story.Status);
        _mockDeveloperStoryRepo.Verify(r => r.UpdateAsync(It.Is<DeveloperStory>(s => s.Status == DeveloperStoryStatus.Ready), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBlockedStoriesAsync_WithNoBlockedStories_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockDeveloperStoryRepo
            .Setup(r => r.GetByStatusAsync(DeveloperStoryStatus.Blocked, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory>());

        // Act
        var result = await _service.GetBlockedStoriesAsync();

        // Assert
        Assert.Empty(result);
    }
}
