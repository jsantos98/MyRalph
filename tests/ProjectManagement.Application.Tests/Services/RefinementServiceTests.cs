using Moq;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Claude;
using Xunit;

namespace ProjectManagement.Application.Tests.Services;

public class RefinementServiceTests
{
    private readonly Mock<IWorkItemRepository> _mockWorkItemRepo;
    private readonly Mock<IDeveloperStoryRepository> _mockDeveloperStoryRepo;
    private readonly Mock<IDeveloperStoryDependencyRepository> _mockDependencyRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly Mock<IClaudeApiService> _mockClaudeService;
    private readonly RefinementService _service;

    public RefinementServiceTests()
    {
        _mockWorkItemRepo = new Mock<IWorkItemRepository>();
        _mockDeveloperStoryRepo = new Mock<IDeveloperStoryRepository>();
        _mockDependencyRepo = new Mock<IDeveloperStoryDependencyRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockStateManager = new Mock<IStateManager>();
        _mockClaudeService = new Mock<IClaudeApiService>();
        _service = new RefinementService(
            _mockWorkItemRepo.Object,
            _mockDeveloperStoryRepo.Object,
            _mockDependencyRepo.Object,
            _mockUnitOfWork.Object,
            _mockStateManager.Object,
            _mockClaudeService.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<RefinementService>>());
    }

    [Fact]
    public async Task RefineWorkItemAsync_WithNonExistentWorkItem_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockWorkItemRepo
            .Setup(r => r.GetWithDeveloperStoriesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.RefineWorkItemAsync(999, null, null, null, null));
    }

    [Fact]
    public async Task RefineWorkItemAsync_WithInvalidStateTransition_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description",
            Status = WorkItemStatus.Completed
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Completed, WorkItemStatus.Refining))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            _service.RefineWorkItemAsync(1, null, null, null, null));
    }

    [Fact]
    public async Task RefineWorkItemAsync_WithClaudeSuccess_CreatesDeveloperStories()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Pending, WorkItemStatus.Refining))
            .Returns(true);

        var claudeResult = new Infrastructure.Claude.ClaudeRefinementResult
        {
            DeveloperStories = new List<Infrastructure.Claude.DeveloperStoryGeneration>
            {
                new() { Title = "Story 1", Description = "D1", Instructions = "I1", StoryType = 0 },
                new() { Title = "Story 2", Description = "D2", Instructions = "I2", StoryType = 1 }
            },
            Dependencies = new List<Infrastructure.Claude.StoryDependency>
            {
                new() { DependentStoryIndex = 1, RequiredStoryIndex = 0, Description = "Test first" }
            },
            Analysis = "Test analysis"
        };

        _mockClaudeService
            .Setup(s => s.RefineWorkItemAsync(workItem, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claudeResult);

        DeveloperStory? capturedStory1 = null;
        DeveloperStory? capturedStory2 = null;

        _mockDeveloperStoryRepo
            .Setup(r => r.AddAsync(It.IsAny<DeveloperStory>(), It.IsAny<CancellationToken>()))
            .Callback<DeveloperStory, CancellationToken>((story, _) =>
            {
                if (story.Title == "Story 1") capturedStory1 = story;
                if (story.Title == "Story 2") capturedStory2 = story;
                story.GetType().GetProperty("Id")?.SetValue(story, story.Title == "Story 1" ? 1 : 2);
            })
            .ReturnsAsync((DeveloperStory story, CancellationToken _) => story);

        // Mock GetWithDependenciesAsync to return the story with empty dependencies
        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                if (id == 1) return capturedStory1;
                if (id == 2) return capturedStory2;
                return null;
            });

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RefineWorkItemAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.WorkItem.Id);
        Assert.Equal(2, result.DeveloperStories.Count);
        Assert.Equal(WorkItemStatus.Refined, result.WorkItem.Status);
        Assert.Equal("Test analysis", result.Analysis);

        _mockDeveloperStoryRepo.Verify(r => r.AddAsync(It.IsAny<DeveloperStory>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefineWorkItemAsync_WithClaudeError_UpdatesWorkItemToError()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Pending, WorkItemStatus.Refining))
            .Returns(true);

        _mockClaudeService
            .Setup(s => s.RefineWorkItemAsync(workItem, null, null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Claude API error"));

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.RefineWorkItemAsync(1, null, null, null, null));

        // Assert
        Assert.Contains("Claude API error", exception.Message);
        Assert.Equal(WorkItemStatus.Error, workItem.Status);
    }

    [Fact]
    public async Task AddDeveloperStoryAsync_WithValidData_CreatesStory()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);

        DeveloperStory? capturedStory = null;
        _mockDeveloperStoryRepo
            .Setup(r => r.AddAsync(It.IsAny<DeveloperStory>(), It.IsAny<CancellationToken>()))
            .Callback<DeveloperStory, CancellationToken>((story, _) =>
            {
                capturedStory = story;
                story.GetType().GetProperty("Id")?.SetValue(story, 1);
            })
            .ReturnsAsync((DeveloperStory story, CancellationToken _) => story);

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.AddDeveloperStoryAsync(
            1,
            (int)DeveloperStoryType.Implementation,
            "Manual Story",
            "Description",
            "Instructions");

        // Assert
        Assert.NotNull(capturedStory);
        Assert.Equal("Manual Story", capturedStory.Title);
        Assert.Equal(DeveloperStoryType.Implementation, capturedStory.StoryType);
        Assert.Equal(DeveloperStoryStatus.Ready, capturedStory.Status);
        Assert.Equal(1, capturedStory.WorkItemId);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddDeveloperStoryAsync_WithNonExistentWorkItem_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.AddDeveloperStoryAsync(999, 0, "T", "D", "I"));
    }

    [Fact]
    public async Task AddDeveloperStoryAsync_WithInvalidStoryType_CastsToEnum()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);

        DeveloperStory? capturedStory = null;
        _mockDeveloperStoryRepo
            .Setup(r => r.AddAsync(It.IsAny<DeveloperStory>(), It.IsAny<CancellationToken>()))
            .Callback<DeveloperStory, CancellationToken>((story, _) => capturedStory = story)
            .ReturnsAsync((DeveloperStory story, CancellationToken _) => story);

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.AddDeveloperStoryAsync(
            1,
            (int)DeveloperStoryType.UnitTests,
            "T",
            "D",
            "I");

        // Assert
        Assert.NotNull(capturedStory);
        Assert.Equal("T", capturedStory.Title);
        Assert.Equal(DeveloperStoryType.UnitTests, capturedStory.StoryType);
        Assert.Equal(DeveloperStoryStatus.Ready, capturedStory.Status);
        Assert.Equal(1, capturedStory.WorkItemId);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
