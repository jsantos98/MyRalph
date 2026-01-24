using Microsoft.Extensions.Options;
using Moq;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Claude;
using ProjectManagement.Infrastructure.Git;
using Xunit;

namespace ProjectManagement.Application.Tests.Services;

public class ImplementationServiceTests
{
    private readonly Mock<IDeveloperStoryRepository> _mockDeveloperStoryRepo;
    private readonly Mock<IWorkItemRepository> _mockWorkItemRepo;
    private readonly Mock<IExecutionLogRepository> _mockExecutionLogRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly Mock<IClaudeCodeIntegration> _mockClaudeCode;
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IOptions<GitSettings>> _mockGitSettings;
    private readonly ImplementationService _service;

    public ImplementationServiceTests()
    {
        _mockDeveloperStoryRepo = new Mock<IDeveloperStoryRepository>();
        _mockWorkItemRepo = new Mock<IWorkItemRepository>();
        _mockExecutionLogRepo = new Mock<IExecutionLogRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockStateManager = new Mock<IStateManager>();
        _mockClaudeCode = new Mock<IClaudeCodeIntegration>();
        _mockGitService = new Mock<IGitService>();
        _mockGitSettings = new Mock<IOptions<GitSettings>>();

        var settings = new GitSettings
        {
            DefaultBranch = "main",
            WorktreeBasePath = "./worktrees"
        };
        _mockGitSettings
            .Setup(s => s.Value)
            .Returns(settings);

        _service = new ImplementationService(
            _mockDeveloperStoryRepo.Object,
            _mockWorkItemRepo.Object,
            _mockExecutionLogRepo.Object,
            _mockUnitOfWork.Object,
            _mockStateManager.Object,
            _mockClaudeCode.Object,
            _mockGitService.Object,
            _mockGitSettings.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ImplementationService>>());
    }

    [Fact]
    public async Task ImplementAsync_WithNonExistentStory_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeveloperStory?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.ImplementAsync(999, "main", "/path", null, null, null, null));
    }

    [Fact]
    public async Task ImplementAsync_WithInvalidStateTransition_ThrowsInvalidStateTransitionException()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Completed
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);
        _mockStateManager
            .Setup(s => s.CanTransition(DeveloperStoryStatus.Completed, DeveloperStoryStatus.InProgress))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            _service.ImplementAsync(1, "main", "/path", null, null, null, null));
    }

    [Fact]
    public async Task ImplementAsync_WithSuccess_CompletesStory()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "Description",
            Instructions = "Do this",
            Status = DeveloperStoryStatus.Ready,
            Priority = 3
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D",
            Status = WorkItemStatus.Refined,
            Priority = 3
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        var otherStories = new List<DeveloperStory>
        {
            new() { WorkItemId = 1, Title = "Other", Description = "D", Instructions = "I", Status = DeveloperStoryStatus.Completed, Priority = 3 }
        };

        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);
        _mockDeveloperStoryRepo
            .Setup(r => r.GetByWorkItemIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherStories);
        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);

        _mockStateManager
            .Setup(s => s.CanTransition(DeveloperStoryStatus.Ready, DeveloperStoryStatus.InProgress))
            .Returns(true);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Refined, WorkItemStatus.InProgress))
            .Returns(true);
        _mockStateManager
            .Setup(s => s.CanTransition(DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Completed))
            .Returns(true);

        _mockGitService
            .Setup(g => g.BranchExistsAsync("/path", "us-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockGitService
            .Setup(g => g.WorktreeExistsAsync("/path", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockClaudeCode
            .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeCodeResult
            {
                ExitCode = 0,
                StandardOutput = "Implementation successful",
                StandardError = "",
                Duration = TimeSpan.FromSeconds(30)
            });

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ImplementAsync(1, "main", "/path");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(DeveloperStoryStatus.Completed, story.Status);
        Assert.NotNull(story.CompletedAt);
        Assert.Equal("us-1", story.GitBranch);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ImplementAsync_WithClaudeCodeFailure_MarksStoryAsError()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "Description",
            Instructions = "Do this",
            Status = DeveloperStoryStatus.Ready,
            Priority = 3
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D",
            Status = WorkItemStatus.InProgress
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);
        _mockDeveloperStoryRepo
            .Setup(r => r.GetByWorkItemIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory>());
        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);

        _mockStateManager
            .Setup(s => s.CanTransition(DeveloperStoryStatus.Ready, DeveloperStoryStatus.InProgress))
            .Returns(true);

        _mockGitService
            .Setup(g => g.BranchExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockGitService
            .Setup(g => g.WorktreeExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockGitService
            .Setup(g => g.GetWorktreePath(story, "./worktrees"))
            .Returns("/worktrees/ds-1");

        _mockClaudeCode
            .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeCodeResult
            {
                ExitCode = 1,
                StandardOutput = "",
                StandardError = "Claude Code failed",
                Duration = TimeSpan.FromSeconds(5)
            });

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ImplementAsync(1, "main", "/path");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(DeveloperStoryStatus.Error, story.Status);
        Assert.Equal("Claude Code failed", story.ErrorMessage);
    }

    [Fact]
    public async Task ImplementAsync_FirstStory_CreatesBranch()
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
            Priority = 3
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D",
            Status = WorkItemStatus.Refined
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockDeveloperStoryRepo
            .Setup(r => r.GetWithDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);
        _mockDeveloperStoryRepo
            .Setup(r => r.GetByWorkItemIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeveloperStory>());
        _mockWorkItemRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);

        _mockStateManager
            .Setup(s => s.CanTransition(DeveloperStoryStatus.Ready, DeveloperStoryStatus.InProgress))
            .Returns(true);
        _mockStateManager
            .Setup(s => s.CanTransition(WorkItemStatus.Refined, WorkItemStatus.InProgress))
            .Returns(true);

        _mockGitService
            .Setup(g => g.BranchExistsAsync("/path", "us-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockGitService
            .Setup(g => g.CreateBranchAsync("/path", "us-1", "main", It.IsAny<CancellationToken>()));
        _mockGitService
            .Setup(g => g.GetWorktreePath(story, "./worktrees"))
            .Returns("/worktrees/ds-1");
        _mockGitService
            .Setup(g => g.WorktreeExistsAsync("/path", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockClaudeCode
            .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeCodeResult { ExitCode = 0, Duration = TimeSpan.Zero });

        _mockUnitOfWork
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.ImplementAsync(1, "main", "/path");

        // Assert
        _mockGitService.Verify(g => g.CreateBranchAsync("/path", "us-1", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureBranchForWorkItemAsync_WithExistingBranch_ReturnsBranchName()
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
        _mockGitService
            .Setup(g => g.BranchExistsAsync("/repo", "us-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.EnsureBranchForWorkItemAsync(1, "/repo", "main");

        // Assert
        Assert.Equal("us-1", result);
        _mockGitService.Verify(g => g.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureBranchForWorkItemAsync_WithNewBranch_CreatesBranch()
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
        _mockGitService
            .Setup(g => g.BranchExistsAsync("/repo", "us-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockGitService
            .Setup(g => g.CreateBranchAsync("/repo", "us-1", "main", It.IsAny<CancellationToken>()));

        // Act
        var result = await _service.EnsureBranchForWorkItemAsync(1, "/repo", "main");

        // Assert
        Assert.Equal("us-1", result);
        _mockGitService.Verify(g => g.CreateBranchAsync("/repo", "us-1", "main", It.IsAny<CancellationToken>()), Times.Once);
    }
}
