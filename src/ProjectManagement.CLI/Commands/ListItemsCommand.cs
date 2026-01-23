using Spectre.Console;
using Spectre.Console.Cli;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;

namespace ProjectManagement.CLI.Commands;

/// <summary>
/// Command to list work items and developer stories
/// </summary>
public class ListItemsCommand : AsyncCommand<ListItemsCommand.Settings>
{
    private readonly IWorkItemService _workItemService;
    private readonly IDeveloperStoryRepository _developerStoryRepository;

    public ListItemsCommand(
        IWorkItemService workItemService,
        IDeveloperStoryRepository developerStoryRepository)
    {
        _workItemService = workItemService;
        _developerStoryRepository = developerStoryRepository;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Stories)
        {
            await ListDeveloperStoriesAsync(settings, cancellationToken);
        }
        else
        {
            await ListWorkItemsAsync(settings, cancellationToken);
        }

        return 0;
    }

    private async Task ListWorkItemsAsync(Settings settings, CancellationToken cancellationToken)
    {
        var workItems = await _workItemService.GetAllAsync(cancellationToken);

        if (settings.Status.HasValue)
        {
            workItems = workItems.Where(wi => wi.Status == settings.Status.Value);
        }

        var workItemList = workItems.ToList();

        if (!workItemList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No work items found[/]");
            return;
        }

        var table = new Table()
            .BorderColor(Color.CornflowerBlue)
            .Border(TableBorder.Rounded)
            .Title($"[bold yellow]Work Items ({workItemList.Count})[/]")
            .AddColumn("[yellow]ID[/]")
            .AddColumn("[yellow]Type[/]")
            .AddColumn("[yellow]Title[/]")
            .AddColumn("[yellow]Status[/]")
            .AddColumn("[yellow]Priority[/]")
            .AddColumn("[yellow]Created[/]");

        foreach (var item in workItemList.OrderByDescending(wi => wi.CreatedAt))
        {
            table.AddRow(
                $"#{item.Id}",
                item.Type.ToString(),
                Truncate(item.Title, 30),
                $"[{GetStatusColor(item.Status)}]{item.Status}[/]",
                GetPriorityBadge(item.Priority),
                item.CreatedAt.ToString("yyyy-MM-dd"));
        }

        AnsiConsole.Write(table);
    }

    private async Task ListDeveloperStoriesAsync(Settings settings, CancellationToken cancellationToken)
    {
        var stories = await _developerStoryRepository.GetAllAsync(cancellationToken);

        if (settings.Status.HasValue)
        {
            var devStatus = settings.Status.Value switch
            {
                WorkItemStatus.Pending => Core.Enums.DeveloperStoryStatus.Pending,
                WorkItemStatus.InProgress => Core.Enums.DeveloperStoryStatus.InProgress,
                WorkItemStatus.Completed => Core.Enums.DeveloperStoryStatus.Completed,
                WorkItemStatus.Error => Core.Enums.DeveloperStoryStatus.Error,
                _ => (Core.Enums.DeveloperStoryStatus?)null
            };

            if (devStatus.HasValue)
            {
                stories = stories.Where(ds => ds.Status == devStatus.Value);
            }
        }

        var storyList = stories.ToList();

        if (!storyList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No developer stories found[/]");
            return;
        }

        var table = new Table()
            .BorderColor(Color.CornflowerBlue)
            .Border(TableBorder.Rounded)
            .Title($"[bold yellow]Developer Stories ({storyList.Count})[/]")
            .AddColumn("[yellow]ID[/]")
            .AddColumn("[yellow]Type[/]")
            .AddColumn("[yellow]Title[/]")
            .AddColumn("[yellow]Status[/]")
            .AddColumn("[yellow]Work Item[/]");

        foreach (var story in storyList.OrderByDescending(ds => ds.Priority).ThenBy(ds => ds.Id))
        {
            table.AddRow(
                $"#{story.Id}",
                $"[{GetStoryTypeColor(story.StoryType)}]{GetStoryTypeIcon(story.StoryType)} {story.StoryType}[/]",
                Truncate(story.Title, 30),
                $"[{GetDevStatusColor(story.Status)}]{story.Status}[/]",
                $"#{story.WorkItemId}");
        }

        AnsiConsole.Write(table);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }

    private static string GetStatusColor(WorkItemStatus status)
    {
        return status switch
        {
            WorkItemStatus.Pending => "yellow",
            WorkItemStatus.Refining => "blue",
            WorkItemStatus.Refined => "cyan",
            WorkItemStatus.InProgress => "yellow",
            WorkItemStatus.Completed => "green",
            WorkItemStatus.Error => "red",
            _ => "white"
        };
    }

    private static string GetDevStatusColor(Core.Enums.DeveloperStoryStatus status)
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

    private static string GetStoryTypeIcon(Core.Enums.DeveloperStoryType type)
    {
        return type switch
        {
            Core.Enums.DeveloperStoryType.Implementation => "",
            Core.Enums.DeveloperStoryType.UnitTests => "",
            Core.Enums.DeveloperStoryType.FeatureTests => "",
            Core.Enums.DeveloperStoryType.Documentation => "",
            _ => " "
        };
    }

    private static string GetPriorityBadge(int priority)
    {
        return priority switch
        {
            1 => "[red on white] 1 [/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + priority + "[/]",
            _ => "[green]" + priority + "[/]"
        };
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-s|--status <STATUS>")]
        public WorkItemStatus? Status { get; set; }

        [CommandOption("--stories")]
        public bool Stories { get; set; }
    }
}
