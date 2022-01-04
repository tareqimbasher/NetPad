using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Runtimes.Assemblies;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using NetPad.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes.Tests
{
    public class ScriptRuntimeTests : TestBase
    {
        public ScriptRuntimeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
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

        [Fact]
        public async Task Must_Be_Initialized()
        {
            var runtime = ServiceProvider.GetRequiredService<IScriptRuntime>();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await runtime.RunAsync(ActionRuntimeInputReader.Null, ActionRuntimeOutputWriter.Null));
        }

        [Fact]
        public async Task Throws_If_Already_Initialized()
        {
            var script = GetScript();
            var runtime = ServiceProvider.GetRequiredService<IScriptRuntime>();
            await runtime.InitializeAsync(script);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await runtime.InitializeAsync(script));
        }

        [Fact]
        public async Task Can_Not_Run_Expression_Kind_Script()
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Expression);
            script.UpdateCode($@"Console.Write(5)");

            var runtime = ServiceProvider.GetRequiredService<IScriptRuntime>();
            await runtime.InitializeAsync(script);

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await runtime.RunAsync(
                    ActionRuntimeInputReader.Null,
                    ActionRuntimeOutputWriter.Null);
            });
        }

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task Can_Run_Statements_Kind_Script(string code, string expectedOutput)
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Statements);
            script.UpdateCode($"Console.Write({code});");

            var runtime = ServiceProvider.GetRequiredService<IScriptRuntime>();
            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Null,
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

            var runtime = ServiceProvider.GetRequiredService<IScriptRuntime>();
            await runtime.InitializeAsync(script);

            string? result = null;

            await runtime.RunAsync(
                ActionRuntimeInputReader.Null,
                new ActionRuntimeOutputWriter(output => result = output?.ToString()));

            Assert.Equal(expectedOutput, result);
        }

        [Fact]
        public async Task Unloads_Assemblies_After_Run()
        {
            int? rollingLoadedAssembliesCount = null;

            for (int i = 0; i < 5; i++)
            {
                var script = GetScript();
                script.Config.SetKind(ScriptKind.Statements);
                script.UpdateCode($"Console.Write(4 + 7);");

                var scope = ServiceProvider.CreateScope();
                var runtime = scope.ServiceProvider.GetRequiredService<IScriptRuntime>();
                await runtime.InitializeAsync(script);

                // Keep result in local variable to test that assembly unloads even if we keep reference to result
                string? result = null;

                await runtime.RunAsync(
                    ActionRuntimeInputReader.Null,
                    new ActionRuntimeOutputWriter((o) =>
                    {
                        result = o?.ToString();
                    }));

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
}
