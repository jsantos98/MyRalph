using Spectre.Console;
using Spectre.Console.Cli;
using ProjectManagement.Application.Services;

namespace ProjectManagement.CLI.Commands;

/// <summary>
/// Command to select and display the next available developer story
/// </summary>
public class SelectNextCommand : AsyncCommand
{
    private readonly IDependencyResolutionService _dependencyResolutionService;

    public SelectNextCommand(IDependencyResolutionService dependencyResolutionService)
    {
        _dependencyResolutionService = dependencyResolutionService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold yellow]Selecting next available story...[/]");

        await _dependencyResolutionService.UpdateDependencyStatusesAsync(cancellationToken);

        var nextStory = await _dependencyResolutionService.SelectNextAsync(cancellationToken);

        if (nextStory == null)
        {
            AnsiConsole.MarkupLine("[yellow]No stories available for implementation[/]");

            // Show blocked stories
            var blockedStories = await _dependencyResolutionService.GetBlockedStoriesAsync(cancellationToken);
            if (blockedStories.Any())
            {
                AnsiConsole.MarkupLine("\n[bold]Blocked Stories:[/]");
                var table = new Table()
                    .BorderColor(Color.CornflowerBlue)
                    .Border(TableBorder.Rounded)
                    .AddColumn("[yellow]Story ID[/]")
                    .AddColumn("[yellow]Title[/]")
                    .AddColumn("[yellow]Blocked By[/]");

                foreach (var (story, blockers) in blockedStories)
                {
                    var blockerList = string.Join(", ", blockers.Select(b => $"#{b.Id}"));
                    table.AddRow($"#{story.Id}", story.Title, blockerList);
                }

                AnsiConsole.Write(table);
            }

            return 0;
        }

        // Display the selected story
        DisplaySelectedStory(nextStory);

        AnsiConsole.MarkupLine($"\n[green]To implement this story, run:[/] [cyan]pm implement {nextStory.Id} <main-branch>[/]");

        return 0;
    }

    private static void DisplaySelectedStory(Core.Entities.DeveloperStory story)
    {
        var panel = new Panel($"[bold]{story.Title}[/]\n\n" +
                             $"[dim]{story.Description}[/]\n\n" +
                             $"[yellow]Instructions:[/]\n[dim]{story.Instructions.Substring(0, Math.Min(200, story.Instructions.Length))}...[/]")
        {
            Header = new PanelHeader($"[green]Next Story: #{story.Id}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("green")
        };
        AnsiConsole.Write(panel);

        var table = new Table()
            .BorderColor(Color.CornflowerBlue)
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Property[/]")
            .AddColumn("[yellow]Value[/]");

        table.AddRow("ID", $"#{story.Id}");
        table.AddRow("Type", $"[{GetStoryTypeColor(story.StoryType)}]{story.StoryType}[/]");
        table.AddRow("Status", $"[{GetStatusColor(story.Status)}]{story.Status}[/]");
        table.AddRow("Priority", story.Priority.ToString());
        table.AddRow("Work Item ID", $"#{story.WorkItemId}");

        AnsiConsole.Write(table);
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
}
