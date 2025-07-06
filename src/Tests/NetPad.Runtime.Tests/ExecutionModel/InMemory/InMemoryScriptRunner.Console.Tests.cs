using Microsoft.Extensions.DependencyInjection;
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
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtime.Tests.ExecutionModel.InMemory;

public class ScriptRuntimeConsoleTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
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

    [Theory(Skip = "InMemoryRunner is outdated")]
    [MemberData(nameof(ConsoleOutputTestData))]
    public async Task ScriptRuntime_Redirects_Console_Output(string code, string expectedOutput)
    {
        var script = ScriptTestHelper.CreateScript();
        script.Config.SetKind(ScriptKind.Program);
        script.UpdateCode($"Console.Write({code});");

        string? result = null;
        var runtime = GetScriptRuntime(script);
        runtime.AddOutput(new ActionOutputWriter<object>((output, title) => result = (output as RawScriptOutput)!.Body!.ToString()));

        await runtime.RunScriptAsync(new RunOptions());

        Assert.Equal(expectedOutput, result);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    private InMemoryScriptRunner GetScriptRuntime(Script script)
    {
        return (InMemoryScriptRunner)ServiceProvider.GetRequiredService<InMemoryScriptRunnerFactory>()
            .CreateRunner(script);
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
