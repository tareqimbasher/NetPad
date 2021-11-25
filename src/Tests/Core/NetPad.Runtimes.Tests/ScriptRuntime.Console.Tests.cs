using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Compilation.CSharp;
using NetPad.Runtimes.Assemblies;
using NetPad.Scripts;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes.Tests
{
    public class ScriptRuntimeConsoleTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ScriptRuntimeConsoleTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
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

            var runtime = GetScriptRuntime();
            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Default,
                new ActionRuntimeOutputWriter(output => result = output?.ToString()));

            _testOutputHelper.WriteLine(result);

            Assert.Equal(expectedOutput, result);
        }

        private IScriptRuntime GetScriptRuntime()
        {
            return new ScriptRuntime(new MainAppDomainAssemblyLoader(), new CSharpCodeParser(), new CSharpCodeCompiler());
        }
    }
}
