using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad;
using NetPad.Apps.Cli;
using NetPad.Apps.Configuration;
using NetPad.Apps.Data;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Scripts;
using NetPad.Configuration;
using NetPad.ExecutionModel;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Packages.NuGet;
using NetPad.Presentation;
using NetPad.Scripts;
using Spectre.Console;

var serviceProvider = BuildServiceProvider();

int returnCode = 0;

var scriptPathArg = new Argument<string>("script", "The script to run.");
scriptPathArg.HelpName = "FILEPATH";

var rootCmd = new RootCommand("NetPad command line tool.");
rootCmd.AddArgument(scriptPathArg);
rootCmd.SetHandler(async path => returnCode = await RunScriptAsync(path), scriptPathArg);
rootCmd.TreatUnmatchedTokensAsErrors = false;

var runCmd = new Command("run", "Run a script");
runCmd.AddArgument(scriptPathArg);
runCmd.SetHandler(async path => returnCode = await RunScriptAsync(path), scriptPathArg);
rootCmd.AddCommand(runCmd);

var listCmd = new Command("list", "List available scripts");
listCmd.SetHandler(() => serviceProvider.GetRequiredService<ScriptFinder>().ListLibraryScripts());
rootCmd.AddCommand(listCmd);

var logsCmd = new Command("logs", "Show logs");
//logsCmd.SetHandler(async () => returnCode = await ShowLogsAsync());
rootCmd.AddCommand(logsCmd);


await rootCmd.InvokeAsync(args);

return returnCode;

IServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();

    services.AddSingleton<ScriptFinder>();

    services.AddLogging();
    services.AddCoreServices();

    services.AddSingleton<Settings>(sp => sp.GetRequiredService<ISettingsRepository>().GetSettingsAsync().Result);
    services.AddTransient<ISettingsRepository, FileSystemSettingsRepository>();
    services.AddTransient<IScriptRepository, FileSystemScriptRepository>();

    // Script execution mechanism
    services.AddExternalExecutionModel(options =>
    {
        options.ProcessCliArgs = args.Skip(1).ToArray();
        options.RedirectIo = false;
    });

    // Data connections
    services
        .AddDataConnectionFeature<
            FileSystemDataConnectionRepository,
            FileSystemDataConnectionResourcesRepository,
            FileSystemDataConnectionResourcesCache>()
        .AddEntityFrameworkCoreDataConnectionDriver();

    // Package management
    services.AddTransient<IPackageProvider, NuGetPackageProvider>();

    return services.BuildServiceProvider(true);
}

async Task<int> RunScriptAsync(string scriptPath)
{
    var scriptRepository = serviceProvider.GetRequiredService<IScriptRepository>();

    Script script;

    try
    {
        script = await scriptRepository.GetAsync(scriptPath);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLineInterpolated($"[red]Could not load script. {ex.Message}[/]");
        return 1;
    }

    using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

    var scriptEnv = new ScriptEnvironment(script, scope);

    scriptEnv.SetIO(new ActionInputReader<string>(Console.ReadLine), new ActionOutputWriter<object>((o, t) =>
    {
        if (o is ErrorScriptOutput errorScriptOutput)
        {
            if (errorScriptOutput.Body != null)
            {
                string text = errorScriptOutput.Body.ToString()!;
                AnsiConsole.MarkupLineInterpolated($"[red]{text}[/]");
            }

            return;
        }

        Console.WriteLine($"{o}");
    }));

    await scriptEnv.RunAsync(new RunOptions());

    return 0;
}

// Customizations for CLI
// 1. No redirect output
// 2. No redirect input
// 3. Different cli args
// 4. Embedded Program.cs args -text/-html...
