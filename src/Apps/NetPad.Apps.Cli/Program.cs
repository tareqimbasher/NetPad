using Microsoft.Extensions.DependencyInjection;
using NetPad;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.EntityFrameworkCore;
using NetPad.ExecutionModel;
using NetPad.Packages;
using NetPad.Packages.NuGet;
using NetPad.Scripts;

if (args.Length == 0)
{
    Console.Error.WriteLine("No script specified.");
    return;
}

var scriptPath = args[0];

if (!File.Exists(scriptPath))
{
    Console.Error.WriteLine($"Script not found: {scriptPath}");
    return;
}

var services = new ServiceCollection();

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

var serviceProvider = services.BuildServiceProvider();

var scriptRepository = serviceProvider.GetRequiredService<IScriptRepository>();

Script script;

try
{
    script = await scriptRepository.GetAsync(scriptPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Could not load script. {ex.Message}");
    return;
}

using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

await new ScriptEnvironment(script, scope).RunAsync(new RunOptions());

// Customizations for CLI
// 1. No redirect output
// 2. No redirect input
// 3. Different cli args
