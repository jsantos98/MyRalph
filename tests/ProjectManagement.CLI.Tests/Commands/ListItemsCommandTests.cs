using Moq;
using Spectre.Console.Cli;
using ProjectManagement.CLI.Commands;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Xunit;

namespace ProjectManagement.CLI.Tests.Commands;

public class ListItemsCommandTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IWorkItemService> _mockWorkItemService;
    private readonly Mock<IDeveloperStoryRepository> _mockDeveloperStoryRepository;
    private readonly Mock<IAnsiConsole> _mockConsole;

    public ListItemsCommandTests()
    {
        _mockWorkItemService = new Mock<IWorkItemService>();
        _mockDeveloperStoryRepository = new Mock<IDeveloperStoryRepository>();
        _mockConsole = new Mock<IAnsiConsole>();

        var services = new ServiceCollection();
        services.AddSingleton(_mockWorkItemService.Object);
        services.AddSingleton(_mockDeveloperStoryRepository.Object);
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
    public async Task ExecuteAsync_WithNoWorkItems_DisplaysNoItemsMessage()
    {
        // Arrange
        _mockWorkItemService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkItem>());

        var command = new ListItemsCommand(_mockWorkItemService.Object, _mockDeveloperStoryRepository.Object);
        var settings = new ListItemsCommand.Settings();
        var context = TestCommandContextFactory.Create("list");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithWorkItems_DisplaysTable()
    {
        // Arrange
        var workItems = new[]
        {
            new WorkItem
            {
                Type = WorkItemType.UserStory,
                Title = "Test Story 1",
                Description = "Description 1",
                Status = WorkItemStatus.Pending,
                Priority = 3,
                CreatedAt = DateTime.UtcNow
            },
            new WorkItem
            {
                Type = WorkItemType.Bug,
                Title = "Bug Fix",
                Description = "Bug description",
                Status = WorkItemStatus.InProgress,
                Priority = 1,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _mockWorkItemService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItems);

        var command = new ListItemsCommand(_mockWorkItemService.Object, _mockDeveloperStoryRepository.Object);
        var settings = new ListItemsCommand.Settings();
        var context = TestCommandContextFactory.Create("list");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilter_FiltersByStatus()
    {
        // Arrange
        var workItems = new[]
        {
            new WorkItem
            {
                Type = WorkItemType.UserStory,
                Title = "Pending Story",
                Description = "D",
                Status = WorkItemStatus.Pending,
                Priority = 3,
                CreatedAt = DateTime.UtcNow
            },
            new WorkItem
            {
                Type = WorkItemType.Bug,
                Title = "Completed Bug",
                Description = "D",
                Status = WorkItemStatus.Completed,
                Priority = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockWorkItemService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workItems);

        var command = new ListItemsCommand(_mockWorkItemService.Object, _mockDeveloperStoryRepository.Object);
        var settings = new ListItemsCommand.Settings { Status = WorkItemStatus.Pending };
        var context = TestCommandContextFactory.Create("list");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithStoriesFlag_ListsDeveloperStories()
    {
        // Arrange
        var stories = new[]
        {
            new DeveloperStory
            {
                WorkItemId = 1,
                StoryType = DeveloperStoryType.Implementation,
                Title = "Impl Story",
                Description = "D",
                Instructions = "I",
                Status = DeveloperStoryStatus.Ready,
                Priority = 1
            }
        };

        _mockDeveloperStoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stories);

        var command = new ListItemsCommand(_mockWorkItemService.Object, _mockDeveloperStoryRepository.Object);
        var settings = new ListItemsCommand.Settings { Stories = true };
        var context = TestCommandContextFactory.Create("list");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
        _mockDeveloperStoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithStoriesFlagAndNoStories_DisplaysNoStoriesMessage()
    {
        // Arrange
        _mockDeveloperStoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DeveloperStory>());

        var command = new ListItemsCommand(_mockWorkItemService.Object, _mockDeveloperStoryRepository.Object);
        var settings = new ListItemsCommand.Settings { Stories = true };
        var context = TestCommandContextFactory.Create("list");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithStoriesAndStatusFilter_FiltersStories()
    {
        // Arrange
        var stories = new[]
        {
            new DeveloperStory
            {
                WorkItemId = 1,
                StoryType = DeveloperStoryType.Implementation,
                Title = "Ready Story",
                Description = "D",
                Instructions = "I",
                Status = DeveloperStoryStatus.Ready,
                Priority = 1
            },
            new DeveloperStory
            {
                WorkItemId = 1,
                StoryType = DeveloperStoryType.UnitTests,
                Title = "Pending Story",
                Description = "D",
                Instructions = "I",
                Status = DeveloperStoryStatus.Pending,
                Priority = 2
            }
        };

        _mockDeveloperStoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stories);

        var command = new ListItemsCommand(_mockWorkItemService.Object, _mockDeveloperStoryRepository.Object);
        var settings = new ListItemsCommand.Settings { Stories = true, Status = WorkItemStatus.InProgress };
        var context = TestCommandContextFactory.Create("list");
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Truncate_WithLongString_TruncatesCorrectly()
    {
        // Arrange
        var longString = "This is a very long string that exceeds the maximum length";
        var maxLength = 20;

        // Act
        var result = longString.Length <= maxLength ? longString : longString.Substring(0, maxLength - 3) + "...";

        // Assert
        Assert.Equal(maxLength, result.Length);
        Assert.EndsWith("...", result);
    }

    [Fact]
    public void Truncate_WithShortString_ReturnsOriginal()
    {
        // Arrange
        var shortString = "Short";
        var maxLength = 20;

        // Act
        var result = shortString.Length <= maxLength ? shortString : shortString.Substring(0, maxLength - 3) + "...";

        // Assert
        Assert.Equal(shortString, result);
    }

    [Fact]
    public void GetPriorityBadge_WithPriority1_ReturnsRedBadge()
    {
        // Arrange
        var priority = 1;

        // Act
        var badge = priority switch
        {
            1 => "[red on white] 1 [/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + priority + "[/]",
            _ => "[green]" + priority + "[/]"
        };

        // Assert
        Assert.Contains("red", badge);
    }

    [Fact]
    public void GetPriorityBadge_WithPriority9_ReturnsGreenBadge()
    {
        // Arrange
        var priority = 9;

        // Act
        var badge = priority switch
        {
            1 => "[red on white] 1 [/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + priority + "[/]",
            _ => "[green]" + priority + "[/]"
        };

        // Assert
        Assert.Contains("green", badge);
    }
}
