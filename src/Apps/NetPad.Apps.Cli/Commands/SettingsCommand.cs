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
        var jsonOption = new Option<bool>("--json", "-j")
        {
            Description = "Print settings as JSON instead of a table.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var settingsCmd = new Command("settings", "View or edit NetPad settings.");
        parent.Subcommands.Add(settingsCmd);
        settingsCmd.Options.Add(jsonOption);
        settingsCmd.SetAction(p => ShowSettings(serviceProvider, p.GetValue(jsonOption)));

        var showCmd = new Command("show", "Display current settings.");
        settingsCmd.Subcommands.Add(showCmd);
        showCmd.Options.Add(jsonOption);
        showCmd.SetAction(p => ShowSettings(serviceProvider, p.GetValue(jsonOption)));

        var editCmd = new Command("edit", "Open settings in your default editor.");
        settingsCmd.Subcommands.Add(editCmd);
        editCmd.SetAction(_ => EditSettingsFile());
    }

    private static int ShowSettings(IServiceProvider serviceProvider, bool jsonFormat)
    {
        if (jsonFormat)
        {
            var settingsPath = AppDataProvider.SettingsFilePath.Path;
            if (!File.Exists(settingsPath))
            {
                Presenter.Error($"Settings file not found: {settingsPath}");
                return 1;
            }

            var json = File.ReadAllText(settingsPath);

            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(json);
            }
            else
            {
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(json);
                }
                catch (JsonException ex)
                {
                    Presenter.Error($"Settings file is not valid JSON: {ex.Message}");
                    return 1;
                }

                using (doc)
                {
                    doc.Dump();
                }
            }
        }
        else
        {
            var settings = serviceProvider.GetRequiredService<Settings>();
            settings.Dump(new ConsoleDumpOptions
            {
                Tables =
                {
                    ShowTitles = false
                }
            });
        }

        return 0;
    }

    private static int EditSettingsFile()
    {
        ProcessUtil.OpenWithDefaultApp(AppDataProvider.SettingsFilePath.Path);
        return 0;
    }
}
