using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;

namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Claude service for work item refinement using Claude Code CLI in non-interactive mode
/// </summary>
public class ClaudeApiService : IClaudeApiService
{
    private readonly ClaudeApiSettings _settings;
    private readonly ILogger<ClaudeApiService> _logger;
    private readonly IClaudeCodeIntegration _claudeCodeIntegration;

    public ClaudeApiService(
        IOptions<ClaudeApiSettings> settings,
        ILogger<ClaudeApiService> logger,
        IClaudeCodeIntegration claudeCodeIntegration)
    {
        _settings = settings.Value;
        _logger = logger;
        _claudeCodeIntegration = claudeCodeIntegration;
    }

    public async Task<ClaudeRefinementResult> RefineWorkItemAsync(
        WorkItem workItem,
        string? apiKey = null,
        string? baseUrl = null,
        int? timeoutMs = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        // Check if Claude Code CLI is available
        var isAvailable = await _claudeCodeIntegration.IsAvailableAsync(cancellationToken);
        if (!isAvailable)
        {
            throw new ClaudeIntegrationException("Claude Code CLI is not available. Please ensure it is installed and accessible.");
        }

        // Use provided model or fall back to settings
        var effectiveModel = model ?? _settings.Model;

        try
        {
            // Get the current working directory
            var workingDirectory = Directory.GetCurrentDirectory();

            _logger.LogInformation("Starting multi-turn refinement for WorkItem {WorkItemId}", workItem.Id);

            // Turn 1: Ask Claude to analyze the work item and identify questions
            var analysisPrompt = BuildAnalysisPrompt(workItem);
            var analysisResult = await _claudeCodeIntegration.StartSessionAsync(
                analysisPrompt,
                workingDirectory,
                apiKey,
                baseUrl,
                timeoutMs,
                effectiveModel,
                cancellationToken);

            if (analysisResult.ExitCode != 0)
            {
                var errorMsg = !string.IsNullOrWhiteSpace(analysisResult.StandardError)
                    ? analysisResult.StandardError
                    : analysisResult.StandardOutput;
                throw new ClaudeIntegrationException($"Claude Code analysis failed with code {analysisResult.ExitCode}: {errorMsg}");
            }

            // Parse the analysis response to get questions
            var analysisContent = ParseCliResponse(analysisResult.StandardOutput);
            _logger.LogInformation("Turn 1 complete. Analysis: {Analysis}", analysisContent.Substring(0, Math.Min(200, analysisContent.Length)));

            // Turn 2: Provide context and ask for the final JSON breakdown
            var refinementPrompt = BuildRefinementPrompt(workItem, analysisContent);
            var sessionId = analysisResult.SessionId ?? Guid.NewGuid().ToString();

            var refinementResult = await _claudeCodeIntegration.ContinueSessionAsync(
                sessionId,
                refinementPrompt,
                workingDirectory,
                apiKey,
                baseUrl,
                timeoutMs,
                effectiveModel,
                cancellationToken);

            if (refinementResult.ExitCode != 0)
            {
                var errorMsg = !string.IsNullOrWhiteSpace(refinementResult.StandardError)
                    ? refinementResult.StandardError
                    : refinementResult.StandardOutput;
                throw new ClaudeIntegrationException($"Claude Code refinement failed with code {refinementResult.ExitCode}: {errorMsg}");
            }

            // Parse the final JSON response
            var resultContent = ParseCliResponse(refinementResult.StandardOutput);

            _logger.LogInformation("Turn 2 response length: {Length} chars", resultContent.Length);
            _logger.LogInformation("Turn 2 response (first 1000 chars): {Content}",
                resultContent.Substring(0, Math.Min(1000, resultContent.Length)));

            // Extract JSON from the result content (handle markdown code blocks)
            var jsonContent = ExtractJsonFromResponse(resultContent);

            var claudeResult = JsonSerializer.Deserialize<ClaudeRefinementResult>(
                jsonContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });

            if (claudeResult == null)
            {
                throw new ClaudeIntegrationException("Failed to parse Claude Code response.");
            }

            // Include the analysis in the result
            claudeResult.Analysis = analysisContent + "\n\n" + (claudeResult.Analysis ?? "");

            _logger.LogInformation("Multi-turn refinement complete. Claude Code returned {Count} developer stories in {Duration}",
                claudeResult.DeveloperStories.Count, analysisResult.Duration + refinementResult.Duration);

            return claudeResult;
        }
        catch (ClaudeIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude Code CLI for WorkItem {WorkItemId}", workItem.Id);
            throw new ClaudeIntegrationException($"Failed to refine work item: {ex.Message}", ex);
        }
    }

    private string BuildAnalysisPrompt(WorkItem workItem)
    {
        var typeText = workItem.Type == WorkItemType.UserStory ? "User Story" : "Bug";
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are an expert software architect and developer. Your task is to analyze a work item and prepare to break it down into detailed developer stories.");
        sb.AppendLine();
        sb.AppendLine("For this work item, I need you to:");
        sb.AppendLine("1. Analyze the requirements and identify what information you need");
        sb.AppendLine("2. List 3-5 clarifying questions that would help create better developer stories");
        sb.AppendLine("3. Identify the specific files, classes, and methods that will likely need to be modified");
        sb.AppendLine("4. Identify any potential technical considerations or edge cases");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT for creating self-contained instructions:");
        sb.AppendLine("- Focus on SPECIFIC files that need to be modified (e.g., 'src/ProjectManagement.CLI/Commands/CreateItemCommand.cs')");
        sb.AppendLine("- Identify the exact classes and methods involved");
        sb.AppendLine("- Consider the existing codebase structure to ensure stories are atomic and focused");
        sb.AppendLine("- Think about which stories can be done independently without touching others");
        sb.AppendLine();
        sb.AppendLine("For each question, explain WHY you need this information and HOW it will help create better, more focused developer stories.");
        sb.AppendLine();
        sb.AppendLine("Do NOT output JSON yet. Just provide your analysis and questions in plain text.");
        sb.AppendLine();
        sb.AppendLine($"# {typeText}: {workItem.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Description:** {workItem.Description}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(workItem.AcceptanceCriteria))
        {
            sb.AppendLine($"**Acceptance Criteria:**");
            sb.AppendLine(workItem.AcceptanceCriteria);
            sb.AppendLine();
        }

        sb.AppendLine($"**Priority:** {workItem.Priority} (1=highest, 9=lowest)");
        sb.AppendLine();
        sb.AppendLine("Please analyze this work item and provide your questions, including specific file paths where relevant.");

        return sb.ToString();
    }

    private string BuildRefinementPrompt(WorkItem workItem, string previousAnalysis)
    {
        var typeText = workItem.Type == WorkItemType.UserStory ? "User Story" : "Bug";
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Based on your previous analysis, break down this work item into detailed developer stories.");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: Make reasonable assumptions for any unanswered questions and proceed with generating the stories.");
        sb.AppendLine("Do not ask more questions - provide actionable developer stories now.");
        sb.AppendLine();
        sb.AppendLine("Story types: 0=Implementation, 1=UnitTests, 2=FeatureTests, 3=Documentation");
        sb.AppendLine();
        sb.AppendLine("For each story, provide title, description, instructions, and storyType.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Instructions must be:");
        sb.AppendLine("- Self-contained (include all file paths and context needed)");
        sb.AppendLine("- Implementation stories ONLY modify implementation files");
        sb.AppendLine("- Test stories ONLY modify test files");
        sb.AppendLine("- Specific about what to change and where");
        sb.AppendLine("- No codebase scanning required");
        sb.AppendLine("- Use plain text for instructions, avoid backslashes in paths");
        sb.AppendLine();
        sb.AppendLine("Dependencies: Use story indices (0-based) to define what must complete first.");
        sb.AppendLine("IMPORTANT: No story should depend on itself (no self-blocking).");
        sb.AppendLine();
        sb.AppendLine("Return ONLY valid JSON matching this structure:");
        sb.AppendLine("{");
        sb.AppendLine("  \"developerStories\": [");
        sb.AppendLine("    { \"title\": \"...\", \"description\": \"...\", \"instructions\": \"...\", \"storyType\": 0 },");
        sb.AppendLine("    ...");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"dependencies\": [");
        sb.AppendLine("    { \"dependentStoryIndex\": 1, \"requiredStoryIndex\": 0, \"description\": \"...\" },");
        sb.AppendLine("    ...");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"analysis\": \"...\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("No markdown code blocks, no extra text - ONLY the JSON object.");

        return sb.ToString();
    }

    private string ParseCliResponse(string content)
    {
        // Parse the Claude Code CLI JSON response to extract the actual result
        var cliResponse = JsonSerializer.Deserialize<ClaudeCodeCliResponse>(content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

        if (cliResponse == null)
        {
            throw new ClaudeIntegrationException("Failed to parse Claude Code CLI response.");
        }

        // Check if there was an error in the CLI execution
        if (!string.IsNullOrWhiteSpace(cliResponse.Error))
        {
            throw new ClaudeIntegrationException($"Claude Code CLI error: {cliResponse.Error}");
        }

        // Extract the actual result content
        var resultContent = cliResponse.Result ?? cliResponse.Output ?? content;

        if (string.IsNullOrWhiteSpace(resultContent))
        {
            throw new ClaudeIntegrationException("Claude Code CLI returned empty result content.");
        }

        return resultContent;
    }

    private string ExtractJsonFromResponse(string content)
    {
        // Look for JSON code blocks
        var jsonBlockStart = content.IndexOf("```json");
        if (jsonBlockStart >= 0)
        {
            var start = jsonBlockStart + 7;
            var end = content.IndexOf("```", start);
            if (end > start)
            {
                return content[start..end].Trim();
            }
        }

        // Look for plain code blocks
        var blockStart = content.IndexOf("```");
        if (blockStart >= 0)
        {
            var start = blockStart + 3;
            var end = content.IndexOf("```", start);
            if (end > start)
            {
                var potentialJson = content[start..end].Trim();
                if (potentialJson.StartsWith("{"))
                {
                    return potentialJson;
                }
            }
        }

        // Try to find JSON object boundaries
        var braceStart = content.IndexOf('{');
        if (braceStart >= 0)
        {
            var braceCount = 0;
            var inString = false;
            var escape = false;

            for (int i = braceStart; i < content.Length; i++)
            {
                var c = content[i];

                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"' && !escape)
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            return content[braceStart..(i + 1)];
                        }
                    }
                }
            }
        }

        return content.Trim();
    }
}

/// <summary>
/// Response structure from Claude Code CLI when using --output-format json
/// </summary>
internal class ClaudeCodeCliResponse
{
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public bool IsError { get; set; }
    public string? Result { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int? ExitCode { get; set; }
}

/// <summary>
/// Claude API settings
/// </summary>
public class ClaudeApiSettings
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4096;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
}
