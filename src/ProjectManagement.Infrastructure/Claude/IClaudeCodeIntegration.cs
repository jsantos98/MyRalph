namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Interface for Claude Code CLI integration
/// </summary>
public interface IClaudeCodeIntegration
{
    /// <summary>
    /// Executes Claude Code non-interactively with a single prompt.
    /// For multi-turn conversations, use StartSessionAsync and ContinueSessionAsync.
    /// </summary>
    Task<ClaudeCodeResult> ExecuteAsync(
        string instruction,
        string workingDirectory,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new Claude Code session for multi-turn conversations.
    /// Returns the session ID that can be used with ContinueSessionAsync.
    /// </summary>
    Task<ClaudeCodeResult> StartSessionAsync(
        string instruction,
        string workingDirectory,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues an existing Claude Code session.
    /// </summary>
    Task<ClaudeCodeResult> ContinueSessionAsync(
        string sessionId,
        string instruction,
        string workingDirectory,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
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

    /// <summary>
    /// Session ID for continuing the conversation (if available)
    /// </summary>
    public string? SessionId { get; set; }
}
