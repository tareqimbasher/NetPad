using Microsoft.Extensions.DependencyInjection;
using NetPad.Compilation;
using NetPad.ExecutionModel.External;
using NetPad.ExecutionModel.InMemory;

namespace NetPad.ExecutionModel;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the "External" execution model. Only one execution model should be registered per-application.
    /// </summary>
    public static void AddExternalExecutionModel(this IServiceCollection services, Action<ExternalScriptRunnerOptions> configure)
    {
        services.AddSingleton<IScriptRunnerFactory, ExternalScriptRunnerFactory>(sp =>
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
    /// Registers the "In Memory" execution model. Only one execution model should be registered per-application.
    /// </summary>
    [Obsolete("Unmaintained and might be removed.")]
    public static void AddInMemoryExecutionModel(this IServiceCollection services)
    {
        services.AddTransient<IScriptRunnerFactory, InMemoryScriptRunnerFactory>();
        services.AddTransient<ICodeParser, InMemoryRunnerCSharpCodeParser>();
    }
}
