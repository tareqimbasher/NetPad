using System.CommandLine;
using System.Text.Json;
using Dumpy.Console;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Apps.Cli.Commands;

public static class SettingsCommand
{
    public static void AddSettingsCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var settingsCmd = new Command("settings", "View and edit NetPad settings.");
        parent.Subcommands.Add(settingsCmd);

        var tableOption = new Option<bool>("--table", "-t")
        {
            Description = "Prints settings in a table format.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var jsonOption = new Option<bool>("--json", "-j")
        {
            Description = "Prints settings as a highlighted JSON string.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        settingsCmd.Options.Add(tableOption);
        settingsCmd.Options.Add(jsonOption);
        settingsCmd.SetAction(p => OpenSettingsFile(
            serviceProvider,
            p.GetValue(tableOption),
            p.GetValue(jsonOption)));

        var editCmd = new Command("edit", "Open settings in your default editor.");
        settingsCmd.Subcommands.Add(editCmd);
        editCmd.SetAction(_ => EditSettingsFile());
    }

    private static int OpenSettingsFile(
        IServiceProvider serviceProvider,
        bool tableFormat = false,
        bool jsonFormat = false)
    {
        if (tableFormat)
        {
            var settings = serviceProvider.GetRequiredService<Settings>();
            settings.Dump();
        }
        else if (jsonFormat)
        {
            JsonDocument.Parse(File.ReadAllText(AppDataProvider.SettingsFilePath.Path)).Dump();
        }
        else
        {
            EditSettingsFile();
        }

        return 0;
    }

    private static int EditSettingsFile()
    {
        ProcessUtil.OpenWithDefaultApp(AppDataProvider.SettingsFilePath.Path);
        return 0;
    }
}
