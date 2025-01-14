using Microsoft.Extensions.DependencyInjection;
using NetPad.Compilation;
using NetPad.ExecutionModel.ClientServer;
using NetPad.ExecutionModel.External;
using NetPad.ExecutionModel.InMemory;

namespace NetPad.ExecutionModel;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the "ClientServer" execution model. Only one execution model should be registered at a time.
    /// </summary>
    public static void AddClientServerExecutionModel(this IServiceCollection services)
    {
        services.AddTransient<IScriptRunnerFactory, ClientServerScriptRunnerFactory>();
        services.AddTransient<ICodeParser, ClientServerCSharpCodeParser>();
    }

    /// <summary>
    /// Registers the "External" execution model. Only one execution model should be registered at a time.
    /// </summary>
    public static void AddExternalExecutionModel(this IServiceCollection services, Action<ExternalScriptRunnerOptions> configure)
    {
        services.AddTransient<IScriptRunnerFactory, ExternalScriptRunnerFactory>(sp =>
        {
            var options = new ExternalScriptRunnerOptions([], false);

            configure(options);

            return new ExternalScriptRunnerFactory(
                sp.GetRequiredService<IServiceProvider>(),
                options
            );
        });
        services.AddTransient<ICodeParser, ExternalRunnerCSharpCodeParser>();
    }

    /// <summary>
    /// Registers the "In Memory" execution model. Only one execution model should be registered at a time.
    /// </summary>
    [Obsolete("Unmaintained and might be removed.")]
    public static void AddInMemoryExecutionModel(this IServiceCollection services)
    {
        services.AddTransient<IScriptRunnerFactory, InMemoryScriptRunnerFactory>();
        services.AddTransient<ICodeParser, InMemoryRunnerCSharpCodeParser>();
    }
}
