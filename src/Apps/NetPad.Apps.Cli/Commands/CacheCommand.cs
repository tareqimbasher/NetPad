using System.CommandLine;
using NetPad.Configuration;
using NetPad.ExecutionModel.External;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class CacheCommand
{
    public static void AddCacheCommand(this RootCommand parent, IServiceProvider _)
    {
        var cacheCmd = new Command("cache", "Show information about the script build cache.");
        parent.Subcommands.Add(cacheCmd);
        cacheCmd.SetAction(_ => ListCachedScriptDeployments());

        var listCmd = new Command("list", "List all script builds.")
        {
            Aliases = { "ls" }
        };
        cacheCmd.Subcommands.Add(listCmd);
        listCmd.SetAction(_ => ListCachedScriptDeployments());

        var removeIdentifierArg = new Argument<string>("number|name")
        {
            Description =
                "A build number (from 'list' output) or a script name to remove.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var removeAllOption = new Option<bool>("--all", "-a")
        {
            Description = "Remove all cached script builds.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var removeCmd = new Command("rm", "Remove a cached script build.");
        cacheCmd.Subcommands.Add(removeCmd);
        removeCmd.Arguments.Add(removeIdentifierArg);
        removeCmd.Options.Add(removeAllOption);
        removeCmd.SetAction(p =>
        {
            var identifier = p.GetValue(removeIdentifierArg);
            var all = p.GetValue(removeAllOption);

            if (!string.IsNullOrEmpty(identifier) && all)
            {
                Presenter.Error("Cannot specify --all when specifying a build to remove.");
                return 1;
            }

            if (string.IsNullOrEmpty(identifier) && !all)
            {
                Presenter.Error(
                    "Specify a number or script name to remove (use 'list' to see cached builds), or --all to remove all.");
                return 1;
            }

            if (all)
            {
                return RemoveAllCachedDeployments();
            }

            // Try parsing as a number first, otherwise treat as a script name
            if (int.TryParse(identifier, out var number))
            {
                return RemoveCachedDeploymentByNumber(number);
            }

            return RemoveCachedDeploymentByName(identifier!);
        });

        var clearCmd = new Command("clear", "Remove all cached script builds.");
        cacheCmd.Subcommands.Add(clearCmd);
        clearCmd.SetAction(_ => RemoveAllCachedDeployments());
    }

    private static int ListCachedScriptDeployments()
    {
        var deployments = GetOrderedDeployments();

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
        foreach (var (dir, info) in deployments)
        {
            table.AddRow(
                new Markup($"[violet]{++order}[/]"),
                new Markup(info.GetScriptName()),
                new Markup(FileSystemUtil.GetReadableFileSize(dir.GetSize())),
                new Markup(info.LastRunAt?.ToString() ?? "Never"),
                new Markup(info.LastRunSucceeded == true ? "[green]success[/]" : "[red]fail[/]")
            );
        }

        AnsiConsole.Write(table);

        return 0;
    }

    private static List<(DeploymentDirectory Directory, DeploymentInfo Info)> GetOrderedDeployments()
    {
        var cache = new DeploymentCache(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath);
        return cache.ListDeploymentDirectories()
            .Select(x => (Directory: x, Info: x.GetDeploymentInfo()!))
            .OrderByDescending(x => x.Info.LastRunAt)
            .ToList();
    }

    private static int RemoveAllCachedDeployments()
    {
        var deployments = GetOrderedDeployments();
        if (deployments.Count == 0)
        {
            AnsiConsole.MarkupLine("cache is empty");
            return 0;
        }

        foreach (var (dir, _) in deployments)
        {
            Try.Run(() => dir.DeleteIfExists());
        }

        AnsiConsole.MarkupLine("[green]success:[/] cache was emptied");
        return 0;
    }

    private static int RemoveCachedDeploymentByNumber(int number)
    {
        var deployments = GetOrderedDeployments();
        if (deployments.Count == 0)
        {
            AnsiConsole.MarkupLine("cache is empty");
            return 0;
        }

        if (number < 1 || number > deployments.Count)
        {
            Presenter.Error($"Invalid build number. Must be between 1 and {deployments.Count}.");
            return 1;
        }

        var (dir, info) = deployments[number - 1];
        Try.Run(() => dir.DeleteIfExists());
        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] cached build for '{info.GetScriptName()}' was removed");
        return 0;
    }

    private static int RemoveCachedDeploymentByName(string name)
    {
        var deployments = GetOrderedDeployments();
        if (deployments.Count == 0)
        {
            AnsiConsole.MarkupLine("cache is empty");
            return 0;
        }

        var matches = deployments
            .Where(x => x.Info.GetScriptName().Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            Presenter.Error($"No cached build matching '{name}' was found.");
            return 1;
        }

        if (matches.Count > 1)
        {
            var exactMatch = matches
                .Where(x => x.Info.GetScriptName().Equals(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactMatch.Count == 1)
            {
                matches = exactMatch;
            }
            else
            {
                Presenter.Error(
                    $"Multiple cached builds match '{name}': {string.Join(", ", matches.Select(x => x.Info.GetScriptName()))}. " +
                    "Use an exact name or a build number.");
                return 1;
            }
        }

        var (dir, info) = matches[0];
        Try.Run(() => dir.DeleteIfExists());
        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] cached build for '{info.GetScriptName()}' was removed");
        return 0;
    }
}
