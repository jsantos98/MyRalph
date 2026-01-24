using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Exceptions;

namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Claude Code CLI integration service with support for multi-turn conversations
/// </summary>
public class ClaudeCodeIntegration : IClaudeCodeIntegration
{
    private readonly ILogger<ClaudeCodeIntegration> _logger;
    private const string ClaudeCodeCommand = "claude";

    public ClaudeCodeIntegration(ILogger<ClaudeCodeIntegration> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ClaudeCodeCommand,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ClaudeCodeResult> ExecuteAsync(
        string instruction,
        string workingDirectory,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        return await StartSessionAsync(instruction, workingDirectory, apiKey, baseUrl, timeoutMs, model, cancellationToken);
    }

    public async Task<ClaudeCodeResult> StartSessionAsync(
        string instruction,
        string workingDirectory,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        // Get effective values from parameters or environment variables
        var effectiveApiKey = GetEffectiveApiKey(apiKey);
        var effectiveBaseUrl = GetEffectiveBaseUrl(baseUrl);
        var effectiveTimeout = GetEffectiveTimeout(timeoutMs);

        // Validate that API key is available
        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            throw new ClaudeIntegrationException(
                "Claude Code API key is not configured. Please set the ANTHROPIC_AUTH_TOKEN environment variable " +
                "or ensure Claude Code is properly authenticated via 'claude login'.");
        }

        _logger.LogInformation("Claude Code API key is configured, base URL: {BaseUrl}",
            string.IsNullOrWhiteSpace(effectiveBaseUrl) ? "default" : effectiveBaseUrl);

        // Build command line arguments for a new session
        var arguments = new List<string>();

        // Add print mode flag (non-interactive)
        arguments.Add("-p");

        // Add output format as JSON for structured parsing
        arguments.Add("--output-format");
        arguments.Add("json");

        // Add model if provided
        if (!string.IsNullOrWhiteSpace(model))
        {
            arguments.Add("--model");
            arguments.Add(model);
        }

        // Add the instruction as the last argument
        arguments.Add(EscapeArgument(instruction));

        var startInfo = new ProcessStartInfo
        {
            FileName = ClaudeCodeCommand,
            Arguments = string.Join(" ", arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        // Set environment variables for the child process
        if (!string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            startInfo.Environment["ANTHROPIC_AUTH_TOKEN"] = effectiveApiKey;
        }
        if (!string.IsNullOrWhiteSpace(effectiveBaseUrl))
        {
            startInfo.Environment["ANTHROPIC_BASE_URL"] = effectiveBaseUrl;
        }

        _logger.LogInformation("Starting Claude Code session in {Directory}", workingDirectory);

        var result = await ExecuteProcessAsync(startInfo, cancellationToken);

        // Try to extract session ID from the response
        result.SessionId = ExtractSessionId(result.StandardOutput);

        return result;
    }

    public async Task<ClaudeCodeResult> ContinueSessionAsync(
        string sessionId,
        string instruction,
        string workingDirectory,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        // Get effective values from parameters or environment variables
        var effectiveApiKey = GetEffectiveApiKey(apiKey);
        var effectiveBaseUrl = GetEffectiveBaseUrl(baseUrl);
        var effectiveTimeout = GetEffectiveTimeout(timeoutMs);

        // Validate that API key is available
        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            throw new ClaudeIntegrationException(
                "Claude Code API key is not configured. Please set the ANTHROPIC_AUTH_TOKEN environment variable.");
        }

        // Build command line arguments for continuing a session
        var arguments = new List<string>();

        // Add print mode flag (non-interactive)
        arguments.Add("-p");

        // Add continue flag (continues the most recent conversation)
        // Note: --continue doesn't use --session-id, that's only for --resume
        arguments.Add("--continue");

        // Add output format as JSON for structured parsing
        arguments.Add("--output-format");
        arguments.Add("json");

        // Add model if provided (can override the model from the previous turn)
        if (!string.IsNullOrWhiteSpace(model))
        {
            arguments.Add("--model");
            arguments.Add(model);
        }

        // Add the instruction as the last argument
        arguments.Add(EscapeArgument(instruction));

        var startInfo = new ProcessStartInfo
        {
            FileName = ClaudeCodeCommand,
            Arguments = string.Join(" ", arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        // Set environment variables for the child process
        if (!string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            startInfo.Environment["ANTHROPIC_AUTH_TOKEN"] = effectiveApiKey;
        }
        if (!string.IsNullOrWhiteSpace(effectiveBaseUrl))
        {
            startInfo.Environment["ANTHROPIC_BASE_URL"] = effectiveBaseUrl;
        }

        _logger.LogInformation("Continuing Claude Code conversation in {Directory}", workingDirectory);

        var result = await ExecuteProcessAsync(startInfo, cancellationToken);

        // Try to extract session ID from the response (may be present in some cases)
        var extractedSessionId = ExtractSessionId(result.StandardOutput);
        result.SessionId = extractedSessionId ?? sessionId;

        return result;
    }

    private async Task<ClaudeCodeResult> ExecuteProcessAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new ClaudeIntegrationException("Failed to start Claude Code process");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync(cancellationToken);

            stopwatch.Stop();

            return new ClaudeCodeResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = await outputTask,
                StandardError = await errorTask,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Claude Code execution was cancelled after {Duration}", stopwatch.Elapsed);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing Claude Code");
            throw new ClaudeIntegrationException($"Failed to execute Claude Code: {ex.Message}", ex);
        }
    }

    private static string? ExtractSessionId(string content)
    {
        try
        {
            // Try to parse the JSON response to extract session_id
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("session_id", out var sessionIdElem) &&
                sessionIdElem.ValueKind == JsonValueKind.String)
            {
                return sessionIdElem.GetString();
            }

            // Also check for uuid field as an alternative
            if (doc.RootElement.TryGetProperty("uuid", out var uuidElem) &&
                uuidElem.ValueKind == JsonValueKind.String)
            {
                return uuidElem.GetString();
            }
        }
        catch
        {
            // If parsing fails, return null
        }

        return null;
    }

    private static string? GetEffectiveApiKey(string? apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");
    }

    private static string? GetEffectiveBaseUrl(string? baseUrl)
    {
        return !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl
            : Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");
    }

    private static int? GetEffectiveTimeout(int? timeoutMs)
    {
        if (timeoutMs.HasValue)
        {
            return timeoutMs.Value;
        }

        var envTimeout = Environment.GetEnvironmentVariable("API_TIMEOUT_MS");
        if (int.TryParse(envTimeout, out var timeout))
        {
            return timeout;
        }

        return null;
    }

    private static string EscapeArgument(string argument)
    {
        // Escape argument for shell - wrap in quotes and escape internal quotes
        return $"\"{argument.Replace("\"", "\\\"")}\"";
    }
}
