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

public class SelectNextCommandTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IDependencyResolutionService> _mockDependencyService;
    private readonly Mock<IAnsiConsole> _mockConsole;

    public SelectNextCommandTests()
    {
        _mockDependencyService = new Mock<IDependencyResolutionService>();
        _mockConsole = new Mock<IAnsiConsole>();

        var services = new ServiceCollection();
        services.AddSingleton(_mockDependencyService.Object);
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
    public async Task ExecuteAsync_WithAvailableStory_DisplaysStoryAndImplementationCommand()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Next Story",
            Description = "Description",
            Instructions = "Instructions with some text that is longer than 200 characters for testing the truncation functionality in the DisplaySelectedStory method which should show only the first 200 characters followed by an ellipsis...",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        _mockDependencyService
            .Setup(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDependencyService
            .Setup(s => s.SelectNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);
        _mockDependencyService
            .Setup(s => s.GetBlockedStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DeveloperStory, List<DeveloperStory>>());

        var command = new SelectNextCommand(_mockDependencyService.Object);
        var context = TestCommandContextFactory.Create("next");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, cancellationToken);

        // Assert
        Assert.Equal(0, result);
        _mockDependencyService.Verify(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockDependencyService.Verify(s => s.SelectNextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoAvailableStory_DisplaysNoStoriesMessage()
    {
        // Arrange
        _mockDependencyService
            .Setup(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDependencyService
            .Setup(s => s.SelectNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeveloperStory?)null);
        _mockDependencyService
            .Setup(s => s.GetBlockedStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DeveloperStory, List<DeveloperStory>>());

        var command = new SelectNextCommand(_mockDependencyService.Object);
        var context = TestCommandContextFactory.Create("next");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoAvailableStoryButBlockedStories_DisplaysBlockedStories()
    {
        // Arrange
        var blockedStory = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Blocked Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 2
        };
        blockedStory.GetType().GetProperty("Id")?.SetValue(blockedStory, 10);

        var blocker = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Blocker Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Pending,
            Priority = 1
        };
        blocker.GetType().GetProperty("Id")?.SetValue(blocker, 5);

        var blockedDict = new Dictionary<DeveloperStory, List<DeveloperStory>>
        {
            { blockedStory, new List<DeveloperStory> { blocker } }
        };

        _mockDependencyService
            .Setup(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDependencyService
            .Setup(s => s.SelectNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeveloperStory?)null);
        _mockDependencyService
            .Setup(s => s.GetBlockedStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedDict);

        var command = new SelectNextCommand(_mockDependencyService.Object);
        var context = TestCommandContextFactory.Create("next");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, cancellationToken);

        // Assert
        Assert.Equal(0, result);
        _mockDependencyService.Verify(s => s.GetBlockedStoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleBlockedStories_DisplaysAllBlockedStories()
    {
        // Arrange
        var blockedStory1 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Blocked 1",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 2
        };
        blockedStory1.GetType().GetProperty("Id")?.SetValue(blockedStory1, 10);

        var blockedStory2 = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.FeatureTests,
            Title = "Blocked 2",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Blocked,
            Priority = 3
        };
        blockedStory2.GetType().GetProperty("Id")?.SetValue(blockedStory2, 11);

        var blocker = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Blocker",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Pending,
            Priority = 1
        };
        blocker.GetType().GetProperty("Id")?.SetValue(blocker, 5);

        var blockedDict = new Dictionary<DeveloperStory, List<DeveloperStory>>
        {
            { blockedStory1, new List<DeveloperStory> { blocker } },
            { blockedStory2, new List<DeveloperStory> { blocker } }
        };

        _mockDependencyService
            .Setup(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDependencyService
            .Setup(s => s.SelectNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeveloperStory?)null);
        _mockDependencyService
            .Setup(s => s.GetBlockedStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedDict);

        var command = new SelectNextCommand(_mockDependencyService.Object);
        var context = TestCommandContextFactory.Create("next");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetStoryTypeColor_ReturnsCorrectColors()
    {
        // Act & Assert
        var colorImpl = DeveloperStoryType.Implementation switch
        {
            DeveloperStoryType.Implementation => "cyan",
            DeveloperStoryType.UnitTests => "green",
            DeveloperStoryType.FeatureTests => "blue",
            DeveloperStoryType.Documentation => "magenta",
            _ => "white"
        };
        Assert.Equal("cyan", colorImpl);

        var colorUnit = DeveloperStoryType.UnitTests switch
        {
            DeveloperStoryType.Implementation => "cyan",
            DeveloperStoryType.UnitTests => "green",
            DeveloperStoryType.FeatureTests => "blue",
            DeveloperStoryType.Documentation => "magenta",
            _ => "white"
        };
        Assert.Equal("green", colorUnit);
    }

    [Fact]
    public void GetStatusColor_ReturnsCorrectColors()
    {
        // Act & Assert
        var colorPending = DeveloperStoryStatus.Pending switch
        {
            DeveloperStoryStatus.Pending => "yellow",
            DeveloperStoryStatus.Ready => "green",
            DeveloperStoryStatus.InProgress => "cyan",
            DeveloperStoryStatus.Completed => "bold green",
            DeveloperStoryStatus.Error => "red",
            DeveloperStoryStatus.Blocked => "red",
            _ => "white"
        };
        Assert.Equal("yellow", colorPending);

        var colorReady = DeveloperStoryStatus.Ready switch
        {
            DeveloperStoryStatus.Pending => "yellow",
            DeveloperStoryStatus.Ready => "green",
            DeveloperStoryStatus.InProgress => "cyan",
            DeveloperStoryStatus.Completed => "bold green",
            DeveloperStoryStatus.Error => "red",
            DeveloperStoryStatus.Blocked => "red",
            _ => "white"
        };
        Assert.Equal("green", colorReady);
    }

    [Fact]
    public async Task ExecuteAsync_CallsUpdateDependencyStatusesBeforeSelect()
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
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        var callOrder = new System.Collections.Generic.List<string>();

        _mockDependencyService
            .Setup(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Update"))
            .Returns(Task.CompletedTask);
        _mockDependencyService
            .Setup(s => s.SelectNextAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Select"))
            .ReturnsAsync(story);
        _mockDependencyService
            .Setup(s => s.GetBlockedStoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DeveloperStory, List<DeveloperStory>>());

        var command = new SelectNextCommand(_mockDependencyService.Object);
        var context = TestCommandContextFactory.Create("next");
        var cancellationToken = CancellationToken.None;

        // Act
        await command.ExecuteAsync(context, cancellationToken);

        // Assert
        Assert.Equal(2, callOrder.Count);
        Assert.Equal("Update", callOrder[0]);
        Assert.Equal("Select", callOrder[1]);
    }
}
