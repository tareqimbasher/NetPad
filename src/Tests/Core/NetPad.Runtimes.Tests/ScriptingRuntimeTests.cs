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
    public class ScriptingRuntimeTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ScriptingRuntimeTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static IEnumerable<object[]> ConsoleOutputTestData2 => new[]
        {
            new[] { "\"Hello World\"", "Hello World" },
            new[] { "(4 + 7).Dump(); 4 + 7", "11" },
            new[] { "4.7 * 2", "9.4" },
            new[] { "DateTime.Today", DateTime.Today.ToString() },
        };

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData2))]
        public async Task Can_Compile_Script(string code, string expectedOutput)
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Statements);
            script.UpdateCode($"{code}");

            var parser = new CSharpScriptCodeParser();
            var compiler = new CSharpScriptCodeCompiler();
            var runtime = new ScriptCodeRuntime(new MainAppDomainAssemblyLoader(), parser, compiler);


            //new InteractiveAssemblyLoader().Dispose();


            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Default,
                new ActionRuntimeOutputWriter(output =>
                {
                    result = output?.ToString();
                    _testOutputHelper.WriteLine(output?.ToString() ?? "NULL");
                }));

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
    }
}
