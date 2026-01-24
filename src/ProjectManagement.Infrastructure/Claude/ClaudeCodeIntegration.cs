using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Exceptions;

namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Claude Code CLI integration service
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
        // Build command line arguments
        var arguments = new List<string>();

        // Add API key if provided
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            arguments.Add("--api-key");
            arguments.Add(EscapeArgument(apiKey));
        }
        else
        {
            // Check environment variable as fallback
            var envApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");
            if (!string.IsNullOrWhiteSpace(envApiKey))
            {
                arguments.Add("--api-key");
                arguments.Add(EscapeArgument(envApiKey));
            }
        }

        // Add base URL if provided
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            arguments.Add("--base-url");
            arguments.Add(EscapeArgument(baseUrl));
        }
        else
        {
            // Check environment variable as fallback
            var envBaseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");
            if (!string.IsNullOrWhiteSpace(envBaseUrl))
            {
                arguments.Add("--base-url");
                arguments.Add(EscapeArgument(envBaseUrl));
            }
        }

        // Add timeout if provided
        if (timeoutMs.HasValue)
        {
            arguments.Add("--timeout");
            arguments.Add(timeoutMs.Value.ToString());
        }
        else
        {
            // Check environment variable as fallback
            var envTimeout = Environment.GetEnvironmentVariable("API_TIMEOUT_MS");
            if (!string.IsNullOrWhiteSpace(envTimeout))
            {
                arguments.Add("--timeout");
                arguments.Add(envTimeout);
            }
        }

        // Add model if provided
        if (!string.IsNullOrWhiteSpace(model))
        {
            arguments.Add("--model");
            arguments.Add(EscapeArgument(model));
        }

        // Add non-interactive flag and instruction
        arguments.Add("--non-interactive");
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

        _logger.LogInformation("Executing Claude Code in {Directory}: {Instruction}",
            workingDirectory, instruction.Substring(0, Math.Min(100, instruction.Length)));

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

    private static string EscapeArgument(string argument)
    {
        // Escape argument for shell - wrap in quotes and escape internal quotes
        return $"\"{argument.Replace("\"", "\\\"")}\"";
    }
}
