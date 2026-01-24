using Spectre.Console;
using Spectre.Console.Cli;
using ProjectManagement.Application.Services;

namespace ProjectManagement.CLI.Commands;

/// <summary>
/// Command to implement a developer story using Claude Code
/// </summary>
public class ImplementCommand : AsyncCommand<ImplementCommand.Settings>
{
    private readonly IImplementationService _implementationService;

    public ImplementCommand(IImplementationService implementationService)
    {
        _implementationService = implementationService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var storyId = settings.StoryId;
        var mainBranch = settings.MainBranch ?? "main";

        AnsiConsole.MarkupLine($"[bold yellow]Implementing Developer Story #{storyId}[/]");
        AnsiConsole.MarkupLine($"[dim]Main branch: {mainBranch}[/]");

        // Get current directory as repository path
        var repositoryPath = Directory.GetCurrentDirectory();

        await AnsiConsole.Progress()
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[bold yellow]Implementing...[/]");

                try
                {
                    var result = await _implementationService.ImplementAsync(
                        storyId,
                        mainBranch,
                        repositoryPath,
                        settings.GetApiKey(),
                        settings.GetBaseUrl(),
                        settings.GetTimeout(),
                        settings.GetModel(),
                        cancellationToken);

                    task.Value = 100;
                    task.StopTask();

                    if (result.Success)
                    {
                        AnsiConsole.MarkupLine($"\n[green]✓[/] Story implemented in [bold]{result.Duration:h\\:mm\\:ss}[/]");

                        if (!string.IsNullOrEmpty(result.Output))
                        {
                            var outputPanel = new Panel($"[dim]{result.Output}[/]")
                            {
                                Header = new PanelHeader("[yellow]Claude Code Output[/]"),
                                Border = BoxBorder.Rounded,
                                BorderStyle = Style.Parse("cornflowerblue"),
                                Expand = true
                            };
                            AnsiConsole.Write(outputPanel);
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"\n[red]✗[/] Implementation failed after {result.Duration:h\\:mm\\:ss}");

                        if (!string.IsNullOrEmpty(result.Error))
                        {
                            var errorPanel = new Panel($"[red]{result.Error}[/]")
                            {
                                Header = new PanelHeader("[red]Error[/]"),
                                Border = BoxBorder.Rounded,
                                BorderStyle = Style.Parse("red"),
                                Expand = true
                            };
                            AnsiConsole.Write(errorPanel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    task.StopTask();
                    AnsiConsole.MarkupLine($"\n[red]✗[/] {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        AnsiConsole.MarkupLine($"[red]  Inner: {ex.InnerException.Message}[/]");
                    }
                }
            });

        return 0;
    }

    public class Settings : ClaudeCommandSettings
    {
        [CommandArgument(0, "<STORY_ID>")]
        public required int StoryId { get; set; }

        [CommandArgument(1, "[MAIN_BRANCH]")]
        public string? MainBranch { get; set; }
    }
}
