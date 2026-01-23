namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Interface for Claude Code CLI integration
/// </summary>
public interface IClaudeCodeIntegration
{
    /// <summary>
    /// Executes Claude Code non-interactively
    /// </summary>
    Task<ClaudeCodeResult> ExecuteAsync(
        string instruction,
        string workingDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Claude Code CLI is available
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Claude Code execution
/// </summary>
public class ClaudeCodeResult
{
    /// <summary>
    /// Exit code from the process
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Standard output
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// Standard error
    /// </summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success => ExitCode == 0;

    /// <summary>
    /// Duration of execution
    /// </summary>
    public TimeSpan Duration { get; set; }
}
