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
    public class ScriptRuntimeTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ScriptRuntimeTests(ITestOutputHelper testOutputHelper)
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

        [Fact]
        public async Task Can_Not_Run_Expression_Kind_Script()
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Expression);
            script.UpdateCode($@"Console.Write(5)");

            var runtime = GetScriptRuntime();
            await runtime.InitializeAsync(script);

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await runtime.RunAsync(
                    ActionRuntimeInputReader.Default,
                    ActionRuntimeOutputWriter.Default);
            });
        }

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task Can_Run_Statements_Kind_Script(string code, string expectedOutput)
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Statements);
            script.UpdateCode($"Console.Write({code});");

            var runtime = GetScriptRuntime();
            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Default,
                new ActionRuntimeOutputWriter(output => result = output?.ToString()));

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task Can_Run_Program_Kind_Script(string code, string expectedOutput)
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Program);
            script.UpdateCode($@"
public async System.Threading.Tasks.Task Main() {{
    Console.Write({code});
}}
");

            var runtime = GetScriptRuntime();
            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Default,
                new ActionRuntimeOutputWriter(output => result = output?.ToString()));

            Assert.Equal(expectedOutput, result);
        }

        private Script GetScript()
        {
            var script = new Script("Test");
            script.Config.SetNamespaces(new[]
            {
                "System"
            });

            return script;
        }

        private IScriptRuntime GetScriptRuntime()
        {
            return new ScriptRuntime(new MainAppDomainAssemblyLoader(), new CSharpCodeParser(), new CSharpCodeCompiler());
        }
    }
}
