using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using Anthropic;

namespace ProjectManagement.Infrastructure.Claude;

/// <summary>
/// Claude API service for work item refinement
/// </summary>
public class ClaudeApiService : IClaudeApiService
{
    private readonly ClaudeApiSettings _settings;
    private readonly ILogger<ClaudeApiService> _logger;

    public ClaudeApiService(
        IOptions<ClaudeApiSettings> settings,
        ILogger<ClaudeApiService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ClaudeRefinementResult> RefineWorkItemAsync(
        WorkItem workItem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ClaudeIntegrationException("Claude API key is not configured.");
        }

        try
        {
            var client = new AnthropicClient(_settings.ApiKey);

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(workItem);

            _logger.LogInformation("Sending refinement request to Claude for WorkItem {WorkItemId}", workItem.Id);

            var parameters = new MessageParameters()
            {
                Model = _settings.Model,
                MaxTokens = _settings.MaxTokens,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                Messages = new List<Message>()
                {
                    new Message(RoleType.User, userPrompt)
                },
                Temperature = 0.3m
            };

            var message = await client.Messages.GetClaudeMessageAsync(parameters, cancellationToken);

            // Get text content from the response
            string content = string.Empty;
            if (message?.Content != null)
            {
                foreach (var contentBlock in message.Content)
                {
                    if (contentBlock != null && contentBlock.GetType().Name == "TextBlock")
                    {
                        var textProperty = contentBlock.GetType().GetProperty("Text");
                        if (textProperty != null)
                        {
                            content += textProperty.GetValue(contentBlock)?.ToString() ?? string.Empty;
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ClaudeIntegrationException("Claude returned empty response.");
            }

            // Extract JSON from response (handle markdown code blocks)
            var jsonContent = ExtractJsonFromResponse(content);

            var result = JsonSerializer.Deserialize<ClaudeRefinementResult>(
                jsonContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });

            if (result == null)
            {
                throw new ClaudeIntegrationException("Failed to parse Claude response.");
            }

            _logger.LogInformation("Claude returned {Count} developer stories", result.DeveloperStories.Count);

            return result;
        }
        catch (ClaudeIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API for WorkItem {WorkItemId}", workItem.Id);
            throw new ClaudeIntegrationException($"Failed to refine work item: {ex.Message}", ex);
        }
    }

    private string BuildSystemPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are an expert software architect and developer. Your task is to break down user stories and bugs into detailed developer stories that can be implemented autonomously.");
        sb.AppendLine();
        sb.AppendLine("For each work item, create a set of developer stories with the following types:");
        sb.AppendLine("- Implementation (0): Main feature/fix implementation");
        sb.AppendLine("- UnitTests (1): Unit tests for the implementation");
        sb.AppendLine("- FeatureTests (2): Integration/feature tests");
        sb.AppendLine("- Documentation (3): Documentation updates");
        sb.AppendLine();
        sb.AppendLine("For each developer story, provide:");
        sb.AppendLine("- title: A concise, descriptive title");
        sb.AppendLine("- description: A brief description of what needs to be done");
        sb.AppendLine("- instructions: Detailed step-by-step implementation instructions");
        sb.AppendLine("- storyType: The type of story (0-3)");
        sb.AppendLine();
        sb.AppendLine("Also identify dependencies between stories using:");
        sb.AppendLine("- dependentStoryIndex: Index of the story that has the dependency");
        sb.AppendLine("- requiredStoryIndex: Index of the story that must complete first");
        sb.AppendLine("- description: Why this dependency exists");
        sb.AppendLine();
        sb.AppendLine("Return your response as a JSON object.");
        sb.AppendLine();
        sb.AppendLine("Ensure that:");
        sb.AppendLine("1. Implementation stories come before their corresponding test stories");
        sb.AppendLine("2. Each story is atomic and can be completed independently (aside from dependencies)");
        sb.AppendLine("3. Instructions are detailed enough for autonomous implementation");
        sb.AppendLine("4. Dependencies are minimized where possible");

        return sb.ToString();
    }

    private string BuildUserPrompt(WorkItem workItem)
    {
        var typeText = workItem.Type == WorkItemType.UserStory ? "User Story" : "Bug";
        var sb = new System.Text.StringBuilder();

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
        sb.AppendLine("Please break this down into developer stories following the specified format.");

        return sb.ToString();
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
