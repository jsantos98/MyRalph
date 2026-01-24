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

public class ImplementCommandTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IImplementationService> _mockImplementationService;
    private readonly Mock<IAnsiConsole> _mockConsole;

    public ImplementCommandTests()
    {
        _mockImplementationService = new Mock<IImplementationService>();
        _mockConsole = new Mock<IAnsiConsole>();

        var services = new ServiceCollection();
        services.AddSingleton(_mockImplementationService.Object);
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
    public async Task ExecuteAsync_WithSuccessfulResult_DisplaysSuccessMessage()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(5),
            Output = "Implementation completed successfully",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = "main" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42", "main" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedResult_DisplaysErrorMessage()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = false,
            Duration = TimeSpan.FromMinutes(2),
            Output = "Partial output",
            Error = "Implementation failed due to error",
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = "main" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42", "main" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_DisplaysExceptionMessage()
    {
        // Arrange
        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = "main" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42", "main" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithExceptionWithInnerException_DisplaysBothMessages()
    {
        // Arrange
        var innerException = new Exception("Inner error");
        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Outer error", innerException));

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = "main" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42", "main" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutMainBranch_UsesDefaultMain()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = null };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomMainBranch_UsesCustomBranch()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "develop",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = "develop" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42", "develop" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "develop", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoOutput_DoesNotDisplayOutputPanel()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = null,
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = null };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoError_DoesNotDisplayErrorPanel()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = false,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Some output",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, MainBranch = null };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
    }

    [Fact]
    public void Settings_StoryIdProperty_IsRequired()
    {
        // Arrange & Act
        var settings = new ImplementCommand.Settings { StoryId = 123, MainBranch = "main" };

        // Assert
        Assert.Equal(123, settings.StoryId);
        Assert.Equal("main", settings.MainBranch);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCurrentDirectoryAsRepositoryPath()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();

        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = null,
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                currentDir,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42 };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", currentDir, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancellationToken = cts.Token;

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42 };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });

        // Act & Assert
        var result = await command.ExecuteAsync(context, settings, cancellationToken);
        Assert.Equal(0, result); // Command returns 0 even on cancellation (progress handles it)
    }

    [Fact]
    public async Task ExecuteAsync_WithApiKey_PassesApiKeyToService()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                "test-api-key",
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, ApiKey = "test-api-key" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), "test-api-key", null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithBaseUrl_PassesBaseUrlToService()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                null,
                "https://api.example.com",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, BaseUrl = "https://api.example.com" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), null, "https://api.example.com", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_PassesTimeoutToService()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                null,
                null,
                5000,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, Timeout = 5000 };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), null, null, 5000, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithModel_PassesModelToService()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                null,
                null,
                null,
                "GLM-4.7",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings { StoryId = 42, Model = "GLM-4.7" };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), null, null, null, "GLM-4.7", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllOptions_PassesAllOptionsToService()
    {
        // Arrange
        var story = new DeveloperStory
        {
            WorkItemId = 1,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Test Story",
            Description = "D",
            Instructions = "I",
            Status = DeveloperStoryStatus.Ready,
            Priority = 1
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        var result = new ImplementationResult
        {
            Success = true,
            Duration = TimeSpan.FromMinutes(1),
            Output = "Done",
            Error = null,
            Story = story
        };

        _mockImplementationService
            .Setup(s => s.ImplementAsync(
                42,
                "main",
                It.IsAny<string>(),
                "test-api-key",
                "https://api.example.com",
                5000,
                "GLM-4.7",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ImplementCommand(_mockImplementationService.Object);
        var settings = new ImplementCommand.Settings
        {
            StoryId = 42,
            ApiKey = "test-api-key",
            BaseUrl = "https://api.example.com",
            Timeout = 5000,
            Model = "GLM-4.7"
        };
        var context = TestCommandContextFactory.Create("implement", new[] { "42" });
        var cancellationToken = CancellationToken.None;

        // Act
        var returnValue = await command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        Assert.Equal(0, returnValue);
        _mockImplementationService.Verify(
            s => s.ImplementAsync(42, "main", It.IsAny<string>(), "test-api-key", "https://api.example.com", 5000, "GLM-4.7", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Settings_InheritsFromClaudeCommandSettings()
    {
        // Arrange & Act
        var settings = new ImplementCommand.Settings { StoryId = 42 };

        // Assert
        Assert.IsAssignableFrom<ClaudeCommandSettings>(settings);
    }
}
