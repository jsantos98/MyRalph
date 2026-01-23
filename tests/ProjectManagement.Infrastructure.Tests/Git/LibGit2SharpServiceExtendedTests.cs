using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Infrastructure.Git;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Git;

public class LibGit2SharpServiceExtendedTests
{
    private readonly Mock<ILogger<LibGit2SharpService>> _mockLogger;
    private readonly GitSettings _settings;

    public LibGit2SharpServiceExtendedTests()
    {
        _mockLogger = new Mock<ILogger<LibGit2SharpService>>();
        _settings = new GitSettings
        {
            DefaultBranch = "main",
            WorktreeBasePath = "./test-worktrees"
        };
    }

    [Fact]
    public void Settings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new GitSettings();

        // Assert
        Assert.Equal("main", settings.DefaultBranch);
        Assert.Equal("./worktrees", settings.WorktreeBasePath);
    }

    [Fact]
    public void Settings_CanSetProperties()
    {
        // Arrange & Act
        var settings = new GitSettings
        {
            DefaultBranch = "develop",
            WorktreeBasePath = "./custom-worktrees"
        };

        // Assert
        Assert.Equal("develop", settings.DefaultBranch);
        Assert.Equal("./custom-worktrees", settings.WorktreeBasePath);
    }

    [Fact]
    public void GetWorktreePath_WithDeveloperStory_ReturnsCorrectPath()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));
        var story = new DeveloperStory
        {
            Title = "Test Story"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 42);

        // Act
        var result = service.GetWorktreePath(story, "./test-worktrees");

        // Assert
        Assert.Equal(Path.Combine("./test-worktrees", "ds-42"), result);
    }

    [Fact]
    public void GetWorktreePath_WithWorktreeBasePathFromSettings_ReturnsExpectedPath()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));
        var story = new DeveloperStory
        {
            Title = "Test Story"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 123);

        // Act
        var result = service.GetWorktreePath(story, _settings.WorktreeBasePath);

        // Assert
        Assert.Equal(Path.Combine(_settings.WorktreeBasePath, "ds-123"), result);
    }

    [Fact]
    public void GetWorktreePath_WithEmptyBasePath_ReturnsWorktreeName()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));
        var story = new DeveloperStory
        {
            Title = "Test"
        };
        story.GetType().GetProperty("Id")?.SetValue(story, 1);

        // Act
        var result = service.GetWorktreePath(story, "");

        // Assert - returns just the story worktree name
        Assert.Equal("ds-1", result);
    }

    [Fact]
    public async Task IsRepositoryAsync_WithNullPath_ReturnsFalse()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service catches exceptions and returns false
        var result = await service.IsRepositoryAsync(null!);
        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WithNullPath_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service wraps exceptions in GitOperationException
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.GetCurrentBranchAsync(null!);
        });
    }

    [Fact]
    public async Task CreateBranchAsync_WithNullRepositoryPath_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service wraps the ArgumentNullException in a GitOperationException
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.CreateBranchAsync(null!, "new-branch", "main");
        });
    }

    [Fact]
    public async Task CreateBranchAsync_WithNullBranchName_ThrowsException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service tries git command which fails because "." is not a git repo
        // The exception may be wrapped in AggregateException or GitOperationException
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await service.CreateBranchAsync(".", null!, "main");
        });
    }

    [Fact]
    public async Task CreateBranchAsync_WithNullFromBranch_ThrowsException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service tries git command which fails because "." is not a git repo
        // The exception may be wrapped in AggregateException or GitOperationException
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await service.CreateBranchAsync(".", "new-branch", null!);
        });
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithNullRepositoryPath_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.CreateWorktreeAsync(null!, "branch", "./worktree");
        });
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithNullBranch_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.CreateWorktreeAsync(".", null!, "./worktree");
        });
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithNullWorktreePath_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.CreateWorktreeAsync(".", "branch", null!);
        });
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WithNullRepositoryPath_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.RemoveWorktreeAsync(null!, "./worktree");
        });
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WithNullWorktreePath_ThrowsGitOperationException()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        await Assert.ThrowsAsync<GitOperationException>(async () =>
        {
            await service.RemoveWorktreeAsync(".", null!);
        });
    }

    [Fact]
    public async Task BranchExistsAsync_WithNullRepositoryPath_ReturnsFalse()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service catches exceptions and returns false
        var result = await service.BranchExistsAsync(null!, "branch");
        Assert.False(result);
    }

    [Fact]
    public async Task BranchExistsAsync_WithNullBranchName_ReturnsFalse()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service returns false for null branch name instead of throwing
        var result = await service.BranchExistsAsync(".", null!);
        Assert.False(result);
    }

    [Fact]
    public async Task WorktreeExistsAsync_WithNullRepositoryPath_ReturnsFalse()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service catches exceptions and returns false
        var result = await service.WorktreeExistsAsync(null!, "worktree");
        Assert.False(result);
    }

    [Fact]
    public async Task WorktreeExistsAsync_WithNullWorktreeName_ReturnsFalse()
    {
        // Arrange
        var service = new LibGit2SharpService(_mockLogger.Object, Options.Create(_settings));

        // Act & Assert
        // The service catches exceptions and returns false
        var result = await service.WorktreeExistsAsync(".", null!);
        Assert.False(result);
    }
}
