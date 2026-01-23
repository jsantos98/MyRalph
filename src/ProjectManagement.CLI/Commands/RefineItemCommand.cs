using Spectre.Console;
using Spectre.Console.Cli;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Interfaces;

namespace ProjectManagement.CLI.Commands;

/// <summary>
/// Command to refine a work item into developer stories
/// </summary>
public class RefineItemCommand : AsyncCommand<RefineItemCommand.Settings>
{
    private readonly IRefinementService _refinementService;
    private readonly IWorkItemService _workItemService;

    public RefineItemCommand(IRefinementService refinementService, IWorkItemService workItemService)
    {
        _refinementService = refinementService;
        _workItemService = workItemService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var workItemId = settings.Id;

        AnsiConsole.MarkupLine($"[bold yellow]Refining Work Item #{workItemId}[/]");

        // Show the work item being refined
        var workItem = await _workItemService.GetWithDeveloperStoriesAsync(workItemId, cancellationToken);
        if (workItem == null)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Work Item #{workItemId} not found");
            return 1;
        }

        DisplayWorkItemSummary(workItem);

        AnsiConsole.MarkupLine("[bold yellow]Calling Claude AI to break down the work item...[/]");

        try
        {
            var result = await _refinementService.RefineWorkItemAsync(workItemId, cancellationToken);

            AnsiConsole.MarkupLine($"[green]✓[/] Refinement completed!");
            AnsiConsole.MarkupLine($"[green]✓[/] Created [bold]{result.DeveloperStories.Count}[/] developer stories");

            if (result.DeveloperStories.Count > 0)
            {
                DisplayDeveloperStories(result.DeveloperStories, result.Dependencies);
            }

            if (!string.IsNullOrEmpty(result.Analysis))
            {
                AnsiConsole.MarkupLine($"\n[bold]Claude's Analysis:[/]");
                AnsiConsole.MarkupLine($"[dim]{result.Analysis}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"\n[red]✗[/] Refinement failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static void DisplayWorkItemSummary(Core.Entities.WorkItem workItem)
    {
        var panel = new Panel($"[bold]{workItem.Title}[/]\n\n[dim]{workItem.Description}[/]")
        {
            Header = new PanelHeader($"[yellow]{workItem.Type} #{workItem.Id}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cornflowerblue")
        };
        AnsiConsole.Write(panel);
    }

    private static void DisplayDeveloperStories(
        List<Core.Entities.DeveloperStory> stories,
        List<Core.Entities.DeveloperStoryDependency> dependencies)
    {
        var tree = new Tree($"[bold yellow]Developer Stories[/]");

        var storyMap = stories.ToDictionary(s => s.Id);

        foreach (var story in stories.OrderBy(s => s.Id))
        {
            var storyNode = tree.AddNode(Markup.FromInterpolated($"[{GetStoryTypeColor(story.StoryType)}]ID:{story.Id}[/] {story.Title}"));

            storyNode.AddNode($"Type: [{GetStoryTypeColor(story.StoryType)}]{story.StoryType}[/]");
            storyNode.AddNode($"Status: [{GetStatusColor(story.Status)}]{story.Status}[/]");
            storyNode.AddNode($"Priority: {story.Priority}");

            if (story.Dependencies.Any())
            {
                var depNode = storyNode.AddNode("[yellow]Dependencies:[/]");
                foreach (var dep in story.Dependencies)
                {
                    if (storyMap.TryGetValue(dep.RequiredStoryId, out var requiredStory))
                    {
                        depNode.AddNode($"→ #{requiredStory.Id}: {requiredStory.Title}");
                    }
                }
            }
        }

        AnsiConsole.Write(tree);
    }

    private static string GetStoryTypeColor(Core.Enums.DeveloperStoryType type)
    {
        return type switch
        {
            Core.Enums.DeveloperStoryType.Implementation => "cyan",
            Core.Enums.DeveloperStoryType.UnitTests => "green",
            Core.Enums.DeveloperStoryType.FeatureTests => "blue",
            Core.Enums.DeveloperStoryType.Documentation => "magenta",
            _ => "white"
        };
    }

    private static string GetStatusColor(Core.Enums.DeveloperStoryStatus status)
    {
        return status switch
        {
            Core.Enums.DeveloperStoryStatus.Pending => "yellow",
            Core.Enums.DeveloperStoryStatus.Ready => "green",
            Core.Enums.DeveloperStoryStatus.InProgress => "cyan",
            Core.Enums.DeveloperStoryStatus.Completed => "bold green",
            Core.Enums.DeveloperStoryStatus.Error => "red",
            Core.Enums.DeveloperStoryStatus.Blocked => "red",
            _ => "white"
        };
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<ID>")]
        public required int Id { get; set; }
    }
}
