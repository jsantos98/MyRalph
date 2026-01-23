using Spectre.Console;
using Spectre.Console.Cli;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Enums;

namespace ProjectManagement.CLI.Commands;

/// <summary>
/// Command to create a new work item
/// </summary>
public class CreateItemCommand : AsyncCommand
{
    private readonly IWorkItemService _workItemService;

    public CreateItemCommand(IWorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold yellow]Create a new Work Item[/]");

        // Select type
        var type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Type of work item?[/]")
                .AddChoices("User Story", "Bug"));

        var workItemType = type == "User Story" ? WorkItemType.UserStory : WorkItemType.Bug;

        // Get title
        var title = AnsiConsole.Ask<string>("[green]Title:[/]");

        // Get description (multi-line)
        AnsiConsole.MarkupLine("[green]Description (press Enter twice to finish):[/]");
        var descriptionLines = new List<string>();
        string? line;
        while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
        {
            descriptionLines.Add(line!);
        }
        var description = string.Join("\n", descriptionLines);

        // Get acceptance criteria (optional)
        var acceptanceCriteria = AnsiConsole.Confirm("[green]Add acceptance criteria?[/]")
            ? await GetMultiLineInput("[green]Acceptance criteria (press Enter twice to finish):[/]")
            : null;

        // Get priority
        var priority = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]Priority (1-9, 1=highest):[/]")
                .ValidationErrorMessage("[red]Priority must be between 1 and 9[/]")
                .Validate(p => p is >= 1 and <= 9));

        // Create the work item
        var workItem = await _workItemService.CreateAsync(
            workItemType,
            title,
            description,
            acceptanceCriteria,
            priority,
            cancellationToken);

        // Display the created work item
        DisplayWorkItem(workItem);

        AnsiConsole.MarkupLine($"[green]✓[/] Created {workItem.Type} with ID [bold]{workItem.Id}[/]");

        return 0;
    }

    private static async Task<string?> GetMultiLineInput(string prompt)
    {
        AnsiConsole.MarkupLine(prompt);
        var lines = new List<string>();
        string? line;
        while (!string.IsNullOrWhiteSpace(line = await Task.Run(() => Console.ReadLine())))
        {
            lines.Add(line!);
        }
        return lines.Count > 0 ? string.Join("\n", lines) : null;
    }

    private static void DisplayWorkItem(Core.Entities.WorkItem workItem)
    {
        var table = new Table()
            .BorderColor(Color.CornflowerBlue)
            .Border(TableBorder.Rounded)
            .Title($"[bold]{workItem.Type} #{workItem.Id}[/]")
            .AddColumn(new TableColumn("[yellow]Field[/]").NoWrap())
            .AddColumn(new TableColumn("[yellow]Value[/]"));

        table.AddRow("ID", workItem.Id.ToString());
        table.AddRow("Type", workItem.Type.ToString());
        table.AddRow("Title", workItem.Title);
        table.AddRow("Description", workItem.Description);
        if (!string.IsNullOrEmpty(workItem.AcceptanceCriteria))
        {
            table.AddRow("Acceptance Criteria", workItem.AcceptanceCriteria);
        }
        table.AddRow("Priority", GetPriorityDisplay(workItem.Priority));
        table.AddRow("Status", $"[{GetStatusColor(workItem.Status)}]{workItem.Status}[/]");
        table.AddRow("Created", workItem.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        AnsiConsole.Write(table);
    }

    private static string GetPriorityDisplay(int priority)
    {
        return priority switch
        {
            1 => "[red]1 (Highest)[/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + priority + "[/]",
            _ => "[green]" + priority + "[/]"
        };
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
}
