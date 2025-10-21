using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps;
using NetPad.Apps.Cli.Commands;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.ExecutionModel;

var serviceProvider = BuildServiceProvider();

var rootCommand = new RootCommand("The NetPad command-line tool.")
{
    TreatUnmatchedTokensAsErrors = false
};

rootCommand.AddRunCommand(serviceProvider);
rootCommand.AddListCommand(serviceProvider);
rootCommand.AddCatCommand(serviceProvider);
rootCommand.AddInfoCommand(serviceProvider);
rootCommand.AddCacheCommand(serviceProvider);
rootCommand.AddLogsCommand(serviceProvider);
rootCommand.AddSettingsCommand(serviceProvider);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();

IServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();

    services.AddCoreServices();
    services.AddLogging();

    // Script execution mechanism
    services.AddExternalExecutionModel();

    // Data connections
    services
        .AddDataConnectionFeature()
        .AddEntityFrameworkCoreDataConnectionDriver();

    return services.BuildServiceProvider(true);
}
