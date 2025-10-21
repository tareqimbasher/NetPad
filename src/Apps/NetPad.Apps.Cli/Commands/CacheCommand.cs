using System.CommandLine;
using NetPad.Configuration;
using NetPad.ExecutionModel.External;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class CacheCommand
{
    public static void AddCacheCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var cacheCmd = new Command("cache", "Show information about the script build cache.");
        parent.Subcommands.Add(cacheCmd);
        cacheCmd.SetAction(_ => ListCachedScriptDeployments(serviceProvider));

        var listCmd = new Command("ls", "List all script builds.");
        cacheCmd.Subcommands.Add(listCmd);
        listCmd.SetAction(_ => ListCachedScriptDeployments(serviceProvider));

        var numberToRemoveArg = new Argument<int?>("number")
        {
            Description =
                "A build number to remove. The number must correspond with the listing shown when running the 'ls' command.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var removeAllOption = new Option<bool>("--all")
        {
            Description = "Remove all cached script builds.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var removeCmd = new Command("rm", "Remove a cached script build.");
        cacheCmd.Subcommands.Add(removeCmd);
        removeCmd.Arguments.Add(numberToRemoveArg);
        removeCmd.Options.Add(removeAllOption);
        removeCmd.SetAction(p =>
        {
            var num = p.GetValue(numberToRemoveArg);
            var all = p.GetValue(removeAllOption);

            if (num.HasValue && all)
            {
                Presenter.Error("Cannot specify --all when specifying a number to remove.");
                return 1;
            }

            if (!num.HasValue && !all)
            {
                Presenter.Error(
                    "Specify a number to remove (use ls to list existing cached builds), or --all to remove all.");
                return 1;
            }

            return RemoveCachedScriptDeployments(num);
        });
    }

    private static int ListCachedScriptDeployments(IServiceProvider serviceProvider)
    {
        var cache = new DeploymentCache(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath);

        var table = new Table
        {
            Border = TableBorder.Rounded,
            ShowHeaders = true,
            BorderStyle = new Style(Color.PaleTurquoise4)
        };

        table.AddColumn(new TableColumn(new Markup("[bold]#[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Script[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Size[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Last Run ▼[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Last Run Result[/]")));

        int order = 0;
        var deployments = cache.ListDeploymentDirectories()
            .Select(x => new
            {
                Directory = x,
                Info = x.GetDeploymentInfo()!
            });

        foreach (var deployment in deployments.OrderByDescending(x => x.Info.LastRunAt))
        {
            table.AddRow(
                new Markup($"[violet]{++order}[/]"),
                new Markup(deployment.Info.GetScriptName()),
                new Markup(FileSystemUtil.GetReadableFileSize(deployment.Directory.GetSize())),
                new Markup(deployment.Info.LastRunAt?.ToString() ?? "Never"),
                new Markup(deployment.Info.LastRunSucceeded == true ? "[green]success[/]" : "[red]fail[/]")
            );
        }

        AnsiConsole.Write(table);

        return 0;
    }

    private static int RemoveCachedScriptDeployments(int? numberToRemove)
    {
        var cache = new DeploymentCache(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath);
        var dirs = cache.ListDeploymentDirectories().ToArray();
        if (dirs.Length == 0)
        {
            AnsiConsole.MarkupLine("cache is empty");
            return 0;
        }

        bool removeAll = numberToRemove == null;

        dirs = numberToRemove.HasValue
            ? dirs.Skip(numberToRemove.Value - 1).Take(1).ToArray()
            : dirs;

        foreach (var dir in dirs)
        {
            Try.Run(() => dir.DeleteIfExists());
        }

        var message = removeAll ? "cache was emptied" : $"cached build was removed";
        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] {message}");
        return 0;
    }
}
