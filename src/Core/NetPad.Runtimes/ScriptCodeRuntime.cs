using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NetPad.Compilation;
using NetPad.Extensions;
using NetPad.Runtimes.Assemblies;
using Script = NetPad.Scripts.Script;

namespace NetPad.Runtimes
{
    public sealed class ScriptCodeRuntime : IScriptRuntime
    {
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly ICodeParser _codeParser;
        private readonly ICodeCompiler _codeCompiler;
        private Script? _script;

        public ScriptCodeRuntime(IAssemblyLoader assemblyLoader, ICodeParser codeParser, ICodeCompiler codeCompiler)
        {
            _assemblyLoader = assemblyLoader;
            _codeParser = codeParser;
            _codeCompiler = codeCompiler;
        }

        public Task InitializeAsync(Script script)
        {
            _script = script;
            return Task.CompletedTask;
        }

        public async Task<bool> RunAsync(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter)
        {
            EnsureInitialization();

            CodeParsingResult parsingResult = _codeParser.Parse(_script!);

            try
            {
                Script<object> csharpScript = GetCSharpScript(parsingResult);

                try
                {
                    ScriptState<object> state = await csharpScript.RunAsync(
                        new ScriptGlobals(outputWriter),
                        (exception) =>
                        {
                            // Possibly do something with exception
                            // ..

                            // Returning true instructs script engine to set the ScriptState.Exception property
                            return true;
                        },
                        CancellationToken.None);

                    if (state.Exception != null)
                    {
                        await outputWriter.WriteAsync(state.Exception.ToString());
                    }
                    else
                    {
                        await outputWriter.WriteAsync(state.ReturnValue?.ToString());
                    }
                }
                catch (CompilationErrorException ex)
                {
                    await outputWriter.WriteAsync(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                await outputWriter.WriteAsync(ex + "\n");
                return false;
            }

            return true;
        }

        private Script<object> GetCSharpScript(CodeParsingResult parsingResult)
        {
            var assemblyLocations = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly =>
                    !assembly.IsDynamic &&
                    !string.IsNullOrWhiteSpace(assembly.Location) &&
                    assembly.GetName()?.Name?.StartsWith("System.") == true)
                .Select(assembly => assembly.Location)
                .ToHashSet();

            assemblyLocations.Add(typeof(IScriptRuntimeOutputWriter).Assembly.Location);

            var references = assemblyLocations
                .Select(location => MetadataReference.CreateFromFile(location));

            var options = ScriptOptions.Default
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithReferences(references)
                .WithEmitDebugInformation(true)
                .WithCheckOverflow(true);

            return CSharpScript.Create(parsingResult.Program, options, typeof(ScriptGlobals));
        }

        private void EnsureInitialization()
        {
            if (_script == null)
                throw new InvalidOperationException($"Script is not initialized.");
        }

        public void Dispose()
        {
            _assemblyLoader.UnloadLoadedAssemblies();
        }
    }

    public class ScriptGlobals
    {
        public ScriptGlobals(IScriptRuntimeOutputWriter outputWriter)
        {
            OutputWriter = outputWriter;
        }

        public IScriptRuntimeOutputWriter OutputWriter { get; }
    }
}
