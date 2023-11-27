using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Assemblies;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using NetPad.Tests.Services;
using NetPad.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes.Tests;

public class ScriptRuntimeTests : TestBase
{
    public ScriptRuntimeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient<ICodeParser, InMemoryRuntimeCSharpCodeParser>();
        services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
        services.AddTransient<IAssemblyLoader, UnloadableAssemblyLoader>();
        services.AddTransient<DefaultInMemoryScriptRuntimeFactory>();
        services.AddTransient<IPackageProvider, NullPackageProvider>();
    }

    public static IEnumerable<object[]> ConsoleOutputTestData => new[]
    {
        new[] { "\"Hello World\"", "Hello World" },
        new[] { "4 + 7", "11" },
        new[] { "4.7 * 2", "9.4" },
        new[] { "DateTime.Today", DateTime.Today.ToString() }
    };

    [Fact]
    public async Task Can_Not_Run_Expression_Kind_Script()
    {
        var script = GetScript();
        script.Config.SetKind(ScriptKind.Expression);
        script.UpdateCode(@"Console.Write(5)");

        var runtime = GetScriptRuntime(script);

        var result = await runtime.RunScriptAsync(new RunOptions());
        Assert.False(result.IsRunAttemptSuccessful);
    }

    [Theory]
    [MemberData(nameof(ConsoleOutputTestData))]
    public async Task Can_Run_Program_Kind_Script(string code, string expectedOutput)
    {
        var script = GetScript();
        script.Config.SetKind(ScriptKind.Program);
        script.UpdateCode($"Console.Write({code});");

        string? result = null;
        var runtime = GetScriptRuntime(script);
        runtime.AddOutput(new ActionOutputWriter<object>((output, title) => result = (output as RawScriptOutput)!.Body!.ToString()));

        var runResult = await runtime.RunScriptAsync(new RunOptions());

        Assert.Equal(expectedOutput, result);
        Assert.True(runResult.IsRunAttemptSuccessful);
    }

    [Fact]
    public async Task Unloads_Assemblies_After_Run()
    {
        int? rollingLoadedAssembliesCount = null;

        for (int i = 0; i < 5; i++)
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Program);
            script.UpdateCode("Console.Write(4 + 7);");

            var scope = ServiceProvider.CreateScope();
            var runtime = GetScriptRuntime(script, scope.ServiceProvider);

            // Keep result in local variable to test that assembly unloads even if we keep reference to result
            string? result = null;
            runtime.AddOutput(new ActionOutputWriter<object>((output, title) => result = (output as RawScriptOutput)!.Body!.ToString()));

            await runtime.RunScriptAsync(new RunOptions());

            scope.Dispose();
            GCUtil.CollectAndWait();

            int loadedAssembliesCount = AppDomain.CurrentDomain.GetAssemblies().Length;
            Logger.LogDebug($"Loaded assemblies count: {loadedAssembliesCount}");

            if (rollingLoadedAssembliesCount == null)
                rollingLoadedAssembliesCount = loadedAssembliesCount;
            else
            {
                Assert.Equal(rollingLoadedAssembliesCount, loadedAssembliesCount);
                rollingLoadedAssembliesCount = loadedAssembliesCount;
            }
        }
    }

    private InMemoryScriptRuntime GetScriptRuntime(Script script, IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= ServiceProvider;

        return (InMemoryScriptRuntime)serviceProvider
            .GetRequiredService<DefaultInMemoryScriptRuntimeFactory>()
            .CreateScriptRuntime(script);
    }

    private Script GetScript()
    {
        var script = ScriptTestHelper.CreateScript();
        script.Config.SetNamespaces(new[]
        {
            "System"
        });

        return script;
    }
}
