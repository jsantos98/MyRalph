using System.Diagnostics;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Exceptions;

namespace ProjectManagement.Infrastructure.Git;

/// <summary>
/// Git service implementation using LibGit2Sharp with process fallback
/// </summary>
public class LibGit2SharpService : IGitService
{
    private readonly ILogger<LibGit2SharpService> _logger;
    private readonly GitSettings _settings;

    public LibGit2SharpService(
        ILogger<LibGit2SharpService> logger,
        IOptions<GitSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<bool> IsRepositoryAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                return Repository.IsValid(path);
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public async Task<string> GetCurrentBranchAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                return repo.Head.FriendlyName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current branch in {Path}", path);
                throw new GitOperationException("Failed to get current branch", ex);
            }
        }, cancellationToken);
    }

    public async Task CreateBranchAsync(
        string repositoryPath,
        string branchName,
        string baseBranch,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(repositoryPath);

                // Get the base branch
                var baseBranchTip = repo.Branches[baseBranch]?.Tip;
                if (baseBranchTip == null)
                {
                    throw new GitOperationException($"Base branch '{baseBranch}' not found");
                }

                // Create the new branch
                var newBranch = repo.CreateBranch(branchName, baseBranchTip);
                _logger.LogInformation("Created branch {Branch} from {BaseBranch}", branchName, baseBranch);
            }
            catch (LibGit2SharpException ex)
            {
                _logger.LogError(ex, "LibGit2Sharp error creating branch {Branch}", branchName);
                // Fallback to git command
                CreateBranchViaCommand(repositoryPath, branchName, baseBranch).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch {Branch}", branchName);
                throw new GitOperationException($"Failed to create branch '{branchName}'", ex, nameof(CreateBranchAsync));
            }
        }, cancellationToken);
    }

    public async Task<string> CreateWorktreeAsync(
        string repositoryPath,
        string branchName,
        string worktreePath,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // LibGit2Sharp doesn't support worktrees directly, use git command
                return CreateWorktreeViaCommand(repositoryPath, branchName, worktreePath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating worktree at {Path}", worktreePath);
                throw new GitOperationException($"Failed to create worktree at '{worktreePath}'", ex, nameof(CreateWorktreeAsync));
            }
        }, cancellationToken);
    }

    public async Task RemoveWorktreeAsync(
        string repositoryPath,
        string worktreePath,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            try
            {
                RemoveWorktreeViaCommand(repositoryPath, worktreePath).GetAwaiter().GetResult();
                _logger.LogInformation("Removed worktree at {Path}", worktreePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing worktree at {Path}", worktreePath);
                throw new GitOperationException($"Failed to remove worktree at '{worktreePath}'", ex, nameof(RemoveWorktreeAsync));
            }
        }, cancellationToken);
    }

    public async Task<bool> BranchExistsAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(repositoryPath);
                return repo.Branches[branchName] != null;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> WorktreeExistsAsync(
        string repositoryPath,
        string worktreePath,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                return Directory.Exists(worktreePath) &&
                       Directory.Exists(Path.Combine(worktreePath, ".git"));
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public string GetWorktreePath(DeveloperStory story, string basePath)
    {
        return Path.Combine(basePath, story.WorktreeName);
    }

    private async Task CreateBranchViaCommand(string repositoryPath, string branchName, string baseBranch)
    {
        var result = await ExecuteGitCommandAsync(
            repositoryPath,
            $"checkout -b {branchName} {baseBranch}");

        if (!result.Success)
        {
            throw new GitOperationException(
                $"Failed to create branch via git command: {result.Error}",
                nameof(CreateBranchViaCommand));
        }
    }

    private async Task<string> CreateWorktreeViaCommand(
        string repositoryPath,
        string branchName,
        string worktreePath)
    {
        // Ensure parent directory exists
        var parentDir = Path.GetDirectoryName(worktreePath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        var result = await ExecuteGitCommandAsync(
            repositoryPath,
            $"worktree add {worktreePath} {branchName}");

        if (!result.Success)
        {
            throw new GitOperationException(
                $"Failed to create worktree via git command: {result.Error}",
                nameof(CreateWorktreeViaCommand));
        }

        return worktreePath;
    }

    private async Task RemoveWorktreeViaCommand(string repositoryPath, string worktreePath)
    {
        var result = await ExecuteGitCommandAsync(
            repositoryPath,
            $"worktree remove {worktreePath}");

        if (!result.Success)
        {
            throw new GitOperationException(
                $"Failed to remove worktree via git command: {result.Error}",
                nameof(RemoveWorktreeViaCommand));
        }
    }

    private async Task<GitCommandResult> ExecuteGitCommandAsync(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return new GitCommandResult { Success = false, Error = "Failed to start git process" };
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new GitCommandResult
        {
            Success = process.ExitCode == 0,
            Output = output,
            Error = error,
            ExitCode = process.ExitCode
        };
    }

    private class GitCommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }
}

/// <summary>
/// Git configuration settings
/// </summary>
public class GitSettings
{
    public const string SectionName = "Git";

    public string DefaultBranch { get; set; } = "main";
    public string WorktreeBasePath { get; set; } = "./worktrees";
}
