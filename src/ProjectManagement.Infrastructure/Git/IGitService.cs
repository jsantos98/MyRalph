using ProjectManagement.Core.Entities;

namespace ProjectManagement.Infrastructure.Git;

/// <summary>
/// Interface for Git operations
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Checks if the current directory is a Git repository
    /// </summary>
    Task<bool> IsRepositoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current branch name
    /// </summary>
    Task<string> GetCurrentBranchAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new branch from the base branch
    /// </summary>
    Task CreateBranchAsync(
        string repositoryPath,
        string branchName,
        string baseBranch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a worktree for a developer story
    /// </summary>
    Task<string> CreateWorktreeAsync(
        string repositoryPath,
        string branchName,
        string worktreePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a worktree
    /// </summary>
    Task RemoveWorktreeAsync(
        string repositoryPath,
        string worktreePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a branch exists
    /// </summary>
    Task<bool> BranchExistsAsync(
        string repositoryPath,
        string branchName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a worktree exists
    /// </summary>
    Task<bool> WorktreeExistsAsync(
        string repositoryPath,
        string worktreePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the worktree path for a developer story
    /// </summary>
    string GetWorktreePath(DeveloperStory story, string basePath);
}
