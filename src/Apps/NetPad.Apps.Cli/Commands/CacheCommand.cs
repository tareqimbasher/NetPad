using System.CommandLine;
using NetPad.Configuration;
using NetPad.ExecutionModel.External;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class CacheCommand
{
    public static void AddCacheCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var cacheCmd = new Command("cache", "Show cache information.");
        parent.Subcommands.Add(cacheCmd);
        cacheCmd.SetAction(_ => ListCachedScriptDeployments(serviceProvider));
    }

    private static int ListCachedScriptDeployments(IServiceProvider serviceProvider)
    {
        var cache = new DeploymentCache(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath);

        var table = new Table()
        {
            Border = TableBorder.Rounded,
            ShowHeaders = true,
            BorderStyle = new Style(Color.PaleTurquoise4)
        };

        table.AddColumn(new TableColumn(new Markup("[bold][olive]#[/][/]")));
        table.AddColumn(new TableColumn(new Markup("[bold][olive]Script[/][/]")));
        table.AddColumn(new TableColumn(new Markup("[bold][olive]Size[/][/]")));
        table.AddColumn(new TableColumn(new Markup("[bold][olive]Last Run[/][/]")));

        int order = 0;
        foreach (var deploymentDir in cache.ListDeploymentDirectories())
        {
            var info = deploymentDir.GetDeploymentInfo();
            var sizeMb = Math.Round(deploymentDir.GetSize() / 1024.0 / 1024.0, 3);
            table.AddRow(
                new Markup($"[blue]{++order}.[/]"),
                new Markup(info!.ScriptAssemblyFileName),
                new Markup($"{sizeMb} MB"),
                new Markup(info.LastRunAt?.ToString() ?? "Never"));
        }

        AnsiConsole.Write(table);

        return 0;
    }
}
