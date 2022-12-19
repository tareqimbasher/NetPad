using Microsoft.Extensions.DependencyInjection;
using NetPad.Compilation;

namespace NetPad.Runtimes;

public static class DependencyInjection
{
    public static void AddExternalProcessScriptRuntime(this IServiceCollection services)
    {
        services.AddTransient<IScriptRuntimeFactory, DefaultExternalProcessScriptRuntimeFactory>();
        services.AddTransient<ICodeParser, ExternalProcessRuntimeCSharpCodeParser>();
    }
}
