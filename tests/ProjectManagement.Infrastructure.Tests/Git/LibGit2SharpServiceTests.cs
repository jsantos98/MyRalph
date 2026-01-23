using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectManagement.Infrastructure.Git;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Git;

public class LibGit2SharpServiceTests : IDisposable
{
    private readonly LibGit2SharpService _service;
    private readonly Mock<ILogger<LibGit2SharpService>> _mockLogger;
    private readonly GitSettings _settings;

    public LibGit2SharpServiceTests()
    {
        _mockLogger = new Mock<ILogger<LibGit2SharpService>>();
        _settings = new GitSettings
        {
            DefaultBranch = "main",
            WorktreeBasePath = "./test-worktrees"
        };
        _service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));
    }

    public void Dispose()
    {
        // Clean up test artifacts if needed
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesService()
    {
        // Assert
        Assert.NotNull(_service);
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsException()
    {
        // Act & Assert
        // The service throws when settings are null
        Assert.ThrowsAny<Exception>(() =>
        {
            new LibGit2SharpService(_mockLogger.Object, null!);
        });
    }

    [Fact]
    public void Constructor_WithNullLogger_CreatesService()
    {
        // Act & Assert
        // The service accepts null logger (handled by null conditional operator internally)
        var service = new LibGit2SharpService(null!, Options.Create(_settings));
        Assert.NotNull(service);
    }

    [Fact]
    public void GetWorktreePath_WithValidStory_ReturnsExpectedPath()
    {
        // Arrange
        var story = new Core.Entities.DeveloperStory
        {
            Title = "Test Story"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        // Act
        var result = _service.GetWorktreePath(story, "./worktrees");

        // Assert
        Assert.Equal(Path.Combine("./worktrees", "ds-42"), result);
    }

    [Fact]
    public void GetWorktreePath_WithWorktreeBasePathFromSettings_ReturnsExpectedPath()
    {
        // Arrange
        var story = new Core.Entities.DeveloperStory
        {
            Title = "Test Story"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 123);

        // Act
        var result = _service.GetWorktreePath(story, _settings.WorktreeBasePath);

        // Assert
        Assert.Equal(Path.Combine(_settings.WorktreeBasePath, "ds-123"), result);
    }

    [Fact]
    public void GetWorktreePath_WithEmptyBasePath_ReturnsWorktreeName()
    {
        // Arrange
        var story = new Core.Entities.DeveloperStory
        {
            Title = "Test"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        // Act
        var result = _service.GetWorktreePath(story, "");

        // Assert - returns just the story worktree name
        Assert.Equal("ds-1", result);
    }
}
