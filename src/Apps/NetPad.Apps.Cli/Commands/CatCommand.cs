using System.CommandLine;
using NetPad.DotNet;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class CatCommand
{
    public static void AddCatCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var catCmd = new Command("cat", "Inspect a script’s metadata and source code.");
        parent.Subcommands.Add(catCmd);

        var pathOrNameArg = new Argument<string>("PATH|NAME")
        {
            Description =
                "A path to a script or text file, or a name (or partial name) to search for in your script library.",
            Arity = ArgumentArity.ExactlyOne,
            HelpName = "PATH|NAME"
        };

        catCmd.Arguments.Add(pathOrNameArg);

        catCmd.SetAction(p => ExecuteAsync(p.GetRequiredValue(pathOrNameArg), serviceProvider));
    }

    private static async Task<int> ExecuteAsync(string pathOrName, IServiceProvider serviceProvider)
    {
        var selectedScriptPath = Helper.SelectScript(serviceProvider, pathOrName);
        if (selectedScriptPath == null) return 1;

        var script = await Helper.LoadScriptFileAsync(serviceProvider, selectedScriptPath, false);
        if (script == null)
        {
            Presenter.Error($"Could not load file: {selectedScriptPath}");
            return 1;
        }

        Print("Path", selectedScriptPath);
        Print("ID", script.Id.ToString());
        Print("Name", script.Name);
        Print("SDK", ".NET " + script.Config.TargetFrameworkVersion.GetMajorVersion());
        Print("Kind", script.Config.Kind.ToString());
        Print("Optimization", script.Config.OptimizationLevel.ToString());
        Print("Data Connection", script.DataConnection?.Name);
        Print("Use ASP.NET", script.Config.UseAspNet.ToString());
        Print("Code", string.Empty);

        var padder = new Padder(new Text(script.Code)).Padding(2, 2, 2, 0);
        AnsiConsole.Write(padder);

        return 0;
    }

    private static void Print(string header, string? text)
    {
        AnsiConsole.MarkupLineInterpolated($"  [b]{header,-20}[/] {text}");
    }
}
