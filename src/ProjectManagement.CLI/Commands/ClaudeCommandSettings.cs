using Spectre.Console.Cli;

namespace ProjectManagement.CLI.Commands;

/// <summary>
/// Base settings class for commands that interact with Claude
/// Supports command-line arguments with environment variable fallback
/// </summary>
public abstract class ClaudeCommandSettings : CommandSettings
{
    /// <summary>
    /// API key for Claude authentication
    /// Falls back to ANTHROPIC_AUTH_TOKEN environment variable
    /// </summary>
    [CommandOption("--api-key <KEY>")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for Claude API
    /// Falls back to ANTHROPIC_BASE_URL environment variable
    /// </summary>
    [CommandOption("--base-url <URL>")]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Timeout in milliseconds for API requests
    /// Falls back to API_TIMEOUT_MS environment variable
    /// </summary>
    [CommandOption("--timeout <MILLISECONDS>")]
    public int? Timeout { get; set; }

    /// <summary>
    /// Model to use for Claude requests
    /// No environment variable fallback for model (user must specify)
    /// </summary>
    [CommandOption("--model <MODEL>")]
    public string? Model { get; set; }

    /// <summary>
    /// Gets the effective API key from command-line argument or environment variable
    /// </summary>
    public string? GetApiKey() => ApiKey ?? Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");

    /// <summary>
    /// Gets the effective base URL from command-line argument or environment variable
    /// </summary>
    public string? GetBaseUrl() => BaseUrl ?? Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");

    /// <summary>
    /// Gets the effective timeout from command-line argument or environment variable
    /// </summary>
    public int? GetTimeout() => Timeout ?? ParseEnvironmentTimeout();

    /// <summary>
    /// Gets the effective model from command-line argument
    /// </summary>
    public string? GetModel() => Model;

    private static int? ParseEnvironmentTimeout()
    {
        var timeoutVar = Environment.GetEnvironmentVariable("API_TIMEOUT_MS");
        if (int.TryParse(timeoutVar, out var timeout))
        {
            return timeout;
        }
        return null;
    }
}
