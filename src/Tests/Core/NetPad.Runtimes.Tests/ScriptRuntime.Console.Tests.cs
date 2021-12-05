using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Runtimes.Assemblies;
using NetPad.Scripts;
using NetPad.Tests;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes.Tests
{
    public class ScriptRuntimeConsoleTests : TestBase
    {
        public ScriptRuntimeConsoleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureServices(ServiceCollection services)
        {
            services.AddTransient<ICodeParser, CSharpCodeParser>();
            services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
            services.AddTransient<IAssemblyLoader, UnloadableAssemblyLoader>();
            services.AddTransient<IScriptRuntime, ScriptRuntime>();
        }

        public static IEnumerable<object[]> ConsoleOutputTestData => new[]
        {
            new[] { "\"Hello World\"", "Hello World" },
            new[] { "4 + 7", "11" },
            new[] { "4.7 * 2", "9.4" },
            new[] { "DateTime.Today", DateTime.Today.ToString() },
        };

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task ScriptRuntime_Redirects_Console_Output(string code, string expectedOutput)
        {
            var script = new Script("Test");
            script.Config.SetKind(ScriptKind.Statements);
            script.UpdateCode($"Console.Write({code});");

            var runtime = ServiceProvider.GetRequiredService<IScriptRuntime>();
            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Default,
                new ActionRuntimeOutputWriter(output => result = output?.ToString()));

            _testOutputHelper.WriteLine(result);

            Assert.Equal(expectedOutput, result);
        }
    }
}
