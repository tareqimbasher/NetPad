using System.CommandLine;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.DotNet;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class InfoCommand
{
    public static void AddInfoCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var infoCmd = new Command("info", "Display information about NetPad and the current environment.");
        parent.Subcommands.Add(infoCmd);
        infoCmd.SetAction(_ => Execute(serviceProvider));
    }

    private static int Execute(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var dotnet = serviceProvider.GetRequiredService<IDotNetInfo>();

        AnsiConsole.MarkupLine("[bold][violet]General[/][/]");
        Print("Version", AppIdentifier.PRODUCT_VERSION);
        Print("Runtime version", RuntimeInformation.FrameworkDescription);
        Print("Runtime identifier", RuntimeInformation.RuntimeIdentifier);
        Print("OS", $"{RuntimeInformation.OSDescription} ({Environment.OSVersion.VersionString})");
        Print("Processor", RuntimeInformation.ProcessArchitecture.ToString());

        AnsiConsole.MarkupLine("\n[bold][violet]Paths[/][/]");
        Print("Scripts library", Helper.ShortenHomePath(settings.ScriptsDirectoryPath));
        Print("App Data directory", Helper.ShortenHomePath(AppDataProvider.AppDataDirectoryPath.Path));
        Print("Logs directory", Helper.ShortenHomePath(AppDataProvider.LogDirectoryPath.Path));
        Print("Settings file", Helper.ShortenHomePath(AppDataProvider.SettingsFilePath.Path));
        Print("Auto-save directory", Helper.ShortenHomePath(settings.AutoSaveScriptsDirectoryPath));
        Print("Package cache", Helper.ShortenHomePath(settings.PackageCacheDirectoryPath));
        Print("CLI build cache",
            Helper.ShortenHomePath(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath.Path));

        // ENV
        AnsiConsole.MarkupLine("\n[bold][violet]Environment[/][/]");
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
            Print(variable, Helper.ShortenHomePath(Environment.GetEnvironmentVariable(variable)));
        }

        AnsiConsole.MarkupLine("\n[bold][violet].NET SDK[/][/]");
        Print("SDK root", Helper.ShortenHomePath(dotnet.LocateDotNetRootDirectory()) ?? "not found");
        Print("SDK root override", Helper.ShortenHomePath(settings.DotNetSdkDirectoryPath));
        Print("dotnet executable", Helper.ShortenHomePath(dotnet.LocateDotNetExecutable()) ?? "not found");

        var efToolPath = dotnet.LocateDotNetEfToolExecutable();
        if (efToolPath == null)
        {
            Print("EF Core CLI tool", "[red]not found[/]");
        }
        else
        {
            var version = dotnet.GetDotNetEfToolVersion(efToolPath);
            efToolPath = Helper.ShortenHomePath(efToolPath);
            Print("EF Core CLI tool", $"{efToolPath} (version: {version})");
        }

        var sdks = dotnet.GetDotNetSdkVersions();
        Print("Detected SDKs", sdks.FirstOrDefault()?.ToString() ?? "none");
        foreach (var sdk in sdks.Skip(1))
        {
            Print(string.Empty, sdk.ToString());
        }


        return 0;
    }

    private static void Print(string header, string? text)
    {
        AnsiConsole.MarkupLineInterpolated($"  [b]{header,-27}[/] {text}");
    }
}
