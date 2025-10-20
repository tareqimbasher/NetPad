using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps;
using NetPad.Apps.Cli;
using NetPad.Apps.Cli.Commands;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.ExecutionModel;

#if !DEBUG
// So app does not get affected by users dev environment
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");
#endif

var serviceProvider = BuildServiceProvider();

var rootCommand = new RootCommand("NetPad command line tool.")
{
    // Extra args are passed to the running script
    TreatUnmatchedTokensAsErrors = false
};

rootCommand.AddRunCommand(serviceProvider);
rootCommand.AddListCommand(serviceProvider);
rootCommand.AddCacheCommand(serviceProvider);
rootCommand.AddLogsCommand(serviceProvider);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();

IServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();

    services.AddCoreServices();
    services.AddLogging();

    // Script execution mechanism
    services.AddExternalExecutionModel(options =>
    {
        options.ProcessCliArgs = args.Skip(2).ToArray();
        options.RedirectIo = false;
    });

    // Data connections
    services
        .AddDataConnectionFeature()
        .AddEntityFrameworkCoreDataConnectionDriver();

    return services.BuildServiceProvider(true);
}
