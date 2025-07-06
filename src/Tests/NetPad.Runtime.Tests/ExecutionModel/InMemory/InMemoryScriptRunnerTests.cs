using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.CodeAnalysis;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.External;
using NetPad.ExecutionModel.InMemory;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using NetPad.Tests.Services;
using NetPad.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtime.Tests.ExecutionModel.InMemory;

public class InMemoryScriptRunnerTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
{
    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient<ICodeParser, ExternalRunnerCSharpCodeParser>();
        services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
        services.AddTransient<InMemoryScriptRunnerFactory>();
        services.AddTransient<IPackageProvider, NullPackageProvider>();
        services.AddTransient<ICodeAnalysisService, CodeAnalysisService>();
    }

    public static IEnumerable<object[]> ConsoleOutputTestData => new[]
    {
        ["\"Hello World\"", "Hello World"],
        ["4 + 7", "11"],
        ["4.7 * 2", "9.4"],
        new[] { "DateTime.Today", DateTime.Today.ToString() }
    };

    [Fact(Skip = "InMemoryRunner is outdated")]
    public async Task Can_Not_Run_Expression_Kind_Script()
    {
        var script = GetScript();
        script.Config.SetKind(ScriptKind.Expression);
        script.UpdateCode(@"Console.Write(5)");

        var runtime = GetScriptRuntime(script);

        var result = await runtime.RunScriptAsync(new RunOptions());
        Assert.False(result.IsRunAttemptSuccessful);
    }

    [Theory(Skip = "InMemoryRunner is outdated")]
    [MemberData(nameof(ConsoleOutputTestData))]
    public async Task Can_Run_Program_Kind_Script(string code, string expectedOutput)
    {
        var script = GetScript();
        script.Config.SetKind(ScriptKind.Program);
        script.UpdateCode($"Console.Write({code});");

        string? result = null;
        var runtime = GetScriptRuntime(script);
        runtime.AddOutput(new ActionOutputWriter<object>((output, title) =>
            result = (output as RawScriptOutput)!.Body!.ToString()));

        var runResult = await runtime.RunScriptAsync(new RunOptions());

        Assert.Equal(expectedOutput, result);
        Assert.True(runResult.IsRunAttemptSuccessful);
    }

    [Fact(Skip = "InMemoryRunner is outdated")]
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
            runtime.AddOutput(new ActionOutputWriter<object>((output, title) =>
                result = (output as RawScriptOutput)!.Body!.ToString()));

            await runtime.RunScriptAsync(new RunOptions());

            scope.Dispose();
            GcUtil.CollectAndWait();

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

#pragma warning disable CS0618 // Type or member is obsolete
    private InMemoryScriptRunner GetScriptRuntime(Script script, IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= ServiceProvider;

        return (InMemoryScriptRunner)serviceProvider
            .GetRequiredService<InMemoryScriptRunnerFactory>()
            .CreateRunner(script);
    }
#pragma warning restore CS0618 // Type or member is obsolete

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
