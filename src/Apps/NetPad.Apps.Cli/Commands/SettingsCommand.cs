using System.CommandLine;
using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Apps.Cli.Commands;

public static class SettingsCommand
{
    public static void AddSettingsCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var settingsCmd = new Command("settings", "NetPad settings.");
        parent.Subcommands.Add(settingsCmd);

        settingsCmd.SetAction(_ => OpenSettingsFile(serviceProvider));
    }

    private static int OpenSettingsFile(IServiceProvider serviceProvider)
    {
        ProcessUtil.OpenWithDefaultApp(AppDataProvider.SettingsFilePath.Path);
        return 0;
    }
}
