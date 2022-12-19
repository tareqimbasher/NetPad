using Microsoft.Extensions.DependencyInjection;
using NetPad.Compilation;

namespace NetPad.Runtimes;

public static class DependencyInjection
{
    public static void AddInMemoryScriptRuntime(this IServiceCollection services)
    {
        services.AddTransient<ICodeParser, InMemoryRuntimeCSharpCodeParser>();
        services.AddTransient<IScriptRuntimeFactory, DefaultInMemoryScriptRuntimeFactory>();
    }
}
