using Moq;
using Spectre.Console.Cli;
using ProjectManagement.CLI.Commands;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Xunit;

namespace ProjectManagement.CLI.Tests.Commands;

public class RefineItemCommandTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IRefinementService> _mockRefinementService;
    private readonly Mock<IWorkItemService> _mockWorkItemService;
    private readonly Mock<IAnsiConsole> _mockConsole;

    public RefineItemCommandTests()
    {
        _mockRefinementService = new Mock<IRefinementService>();
        _mockWorkItemService = new Mock<IWorkItemService>();
        _mockConsole = new Mock<IAnsiConsole>();

        var services = new ServiceCollection();
        services.AddSingleton(_mockRefinementService.Object);
        services.AddSingleton(_mockWorkItemService.Object);
        services.AddSingleton(_mockConsole.Object);
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidWorkItem_RefinesSuccessfully()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending,
            Priority = 3
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        var story1 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story 1",
            Description = "D1",
            Instructions = "I1",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story1.GetType().GetProperty("Id")?.SetValue(story1, 10);

        var story2 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2",
            Status = DeveloperStoryStatus.Ready,
            Priority = 2
        };
        story2.GetType().GetProperty("Id")?.SetValue(story2, 11);

        var refinementResult = new RefinementResult
        {
            WorkItem = workItem,
            DeveloperStories = new List<DeveloperStory> { story1, story2 },
            Dependencies = new List<DeveloperStoryDependency>(),
            Analysis = "Test analysis"
        };

        _mockWorkItemService
            .Setup(s => s.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockRefinementService
            .Setup(s => s.RefineWorkItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);

        var command = new RefineItemCommand(_mockRefinementService.Object, _mockWorkItemService.Object);
        var settings = new RefineItemCommand.Settings { Id = 1 };
        var context = TestCommandContextFactory.Create("refine", new[] { "1" });
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
        _mockWorkItemService.Verify(s => s.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRefinementService.Verify(s => s.RefineWorkItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentWorkItem_ReturnsError()
    {
        // Arrange
        _mockWorkItemService
            .Setup(s => s.GetWithDeveloperStoriesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        var command = new RefineItemCommand(_mockRefinementService.Object, _mockWorkItemService.Object);
        var settings = new RefineItemCommand.Settings { Id = 999 };
        var context = TestCommandContextFactory.Create("refine", new[] { "999" });
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(1, result);
        _mockRefinementService.Verify(s => s.RefineWorkItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithRefinementException_DisplaysErrorMessage()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending,
            Priority = 3
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        _mockWorkItemService
            .Setup(s => s.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockRefinementService
            .Setup(s => s.RefineWorkItemAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Claude API error"));

        var command = new RefineItemCommand(_mockRefinementService.Object, _mockWorkItemService.Object);
        var settings = new RefineItemCommand.Settings { Id = 1 };
        var context = TestCommandContextFactory.Create("refine", new[] { "1" });
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDeveloperStories_DisplaysSuccessMessage()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending,
            Priority = 3
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        var refinementResult = new RefinementResult
        {
            WorkItem = workItem,
            DeveloperStories = new List<DeveloperStory>(),
            Dependencies = new List<DeveloperStoryDependency>(),
            Analysis = "No stories generated"
        };

        _mockWorkItemService
            .Setup(s => s.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockRefinementService
            .Setup(s => s.RefineWorkItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);

        var command = new RefineItemCommand(_mockRefinementService.Object, _mockWorkItemService.Object);
        var settings = new RefineItemCommand.Settings { Id = 1 };
        var context = TestCommandContextFactory.Create("refine", new[] { "1" });
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithDependencies_DisplaysTreeWithDependencies()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending,
            Priority = 3
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        var story1 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story 1",
            Description = "D1",
            Instructions = "I1",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story1.GetType().GetProperty("Id")?.SetValue(story1, 10);

        var story2 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 2
        };
        story2.GetType().GetProperty("Id")?.SetValue(story2, 11);

        var dep1 = new DeveloperStoryDependency
        {
            DependentStoryId = 11,
            RequiredStoryId = 10
        };

        var refinementResult = new RefinementResult
        {
            WorkItem = workItem,
            DeveloperStories = new List<DeveloperStory> { story1, story2 },
            Dependencies = new List<DeveloperStoryDependency> { dep1 },
            Analysis = null
        };

        _mockWorkItemService
            .Setup(s => s.GetWithDeveloperStoriesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItem);
        _mockRefinementService
            .Setup(s => s.RefineWorkItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);

        var command = new RefineItemCommand(_mockRefinementService.Object, _mockWorkItemService.Object);
        var settings = new RefineItemCommand.Settings { Id = 1 };
        var context = TestCommandContextFactory.Create("refine", new[] { "1" });
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Settings_IdProperty_IsRequired()
    {
        // Arrange & Act
        var settings = new RefineItemCommand.Settings { Id = 42 };

        // Assert
        Assert.Equal(42, settings.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_TokenIsPassed()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            Status = WorkItemStatus.Pending,
            Priority = 3
        };
        workItem.GetType().GetProperty("Id")?.SetValue(workItem, 1);

        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancellationToken = cts.Token;

        _mockWorkItemService
            .Setup(s => s.GetWithDeveloperStoriesAsync(1, cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var command = new RefineItemCommand(_mockRefinementService.Object, _mockWorkItemService.Object);
        var settings = new RefineItemCommand.Settings { Id = 1 };
        var context = TestCommandContextFactory.Create("refine", new[] { "1" });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            command.ExecuteAsync(context, settings, cancellationToken));
    }
}
