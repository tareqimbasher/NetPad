using System.CommandLine;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class InfoCommand
{
    private static readonly string _homePath =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('/');

    public static void AddInfoCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var infoCmd = new Command("info", "Prints information about NetPad and the current environment.");
        parent.Subcommands.Add(infoCmd);
        infoCmd.SetAction(_ => Execute(serviceProvider));
    }

    private static int Execute(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var dotnet = serviceProvider.GetRequiredService<IDotNetInfo>();

        AnsiConsole.MarkupLine("[bold]General[/]");
        Print("Version", AppIdentifier.PRODUCT_VERSION);
        Print("Runtime version", RuntimeInformation.FrameworkDescription);
        Print("Runtime identifier", RuntimeInformation.RuntimeIdentifier);
        Print("OS", $"{RuntimeInformation.OSDescription} ({Environment.OSVersion.VersionString})");
        Print("Processor", RuntimeInformation.ProcessArchitecture.ToString());

        AnsiConsole.MarkupLine("\n[bold]Paths[/]");
        Print("Scripts library", RemoveHomeDirPath(settings.ScriptsDirectoryPath));
        Print("App Data directory", RemoveHomeDirPath(AppDataProvider.AppDataDirectoryPath.Path));
        Print("Logs directory", RemoveHomeDirPath(AppDataProvider.LogDirectoryPath.Path));
        Print("Settings file", RemoveHomeDirPath(AppDataProvider.SettingsFilePath.Path));
        Print("Auto-save directory", RemoveHomeDirPath(settings.AutoSaveScriptsDirectoryPath));
        Print("Package cache", RemoveHomeDirPath(settings.PackageCacheDirectoryPath));
        Print("CLI build cache", RemoveHomeDirPath(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath.Path));

        // ENV
        AnsiConsole.MarkupLine("\n[bold]Environment Variables[/]");
        string[] variables =
        [
            "NETPAD_CACHE_DIR",

            "DOTNET_ROOT",
            "DOTNET_ROOT_X64",
            "DOTNET_ROOT_ARM64",
            "DOTNET_HOST_PATH",
            "DOTNET_ENVIRONMENT",
            "ASPNETCORE_ENVIRONMENT",
            "DOTNET_ROLL_FORWARD",
            "NUGET_PACKAGES",
        ];

        foreach (var variable in variables)
        {
            Print(variable, RemoveHomeDirPath(Environment.GetEnvironmentVariable(variable)));
        }

        AnsiConsole.MarkupLine("\n[bold].NET SDK[/]");
        Print("SDK root", RemoveHomeDirPath(dotnet.LocateDotNetRootDirectory()));
        Print("SDK root override", RemoveHomeDirPath(settings.DotNetSdkDirectoryPath));
        Print("EF Core CLI tool", RemoveHomeDirPath(dotnet.LocateDotNetEfToolExecutable()));

        return 0;
    }

    private static void Print(string header, string? text)
    {
        AnsiConsole.MarkupLineInterpolated($"  [b][violet]{header,-27}[/][/] {text}");
    }

    private static string? RemoveHomeDirPath(string? path)
    {
        if (path == null)
        {
            return null;
        }

        var index = path.IndexOf(_homePath, StringComparison.InvariantCultureIgnoreCase);
        if (index < 0)
        {
            return path;
        }

        return $"~{path[_homePath.Length..]}";
    }
}
