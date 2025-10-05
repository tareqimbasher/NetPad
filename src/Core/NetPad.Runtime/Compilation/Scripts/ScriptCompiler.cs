using System.Reflection;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.DotNet;
using NetPad.DotNet.CodeAnalysis;
using NetPad.DotNet.References;
using NetPad.Scripts;

namespace NetPad.Compilation.Scripts;

public class ScriptCompiler(
    IScriptDependencyResolver scriptDependencyResolver,
    ICodeParser codeParser,
    ICodeCompiler codeCompiler) : IScriptCompiler
{
    // We will try different permutations of the user's code, starting with compiling it as-is. The idea is to account
    // for, and give the ability for users to, run expressions not ending with semi-colon or .Dump() and generate
    // the "missing" pieces to run the expression.
    // TODO use SyntaxTree to determine if expression or not. Currently not a performance bottleneck however.
    private static readonly List<Func<string, (bool shouldAttempt, string code)>> _permutations =
    [
        // First try user's code as-is
        code => (true, code),

        // Try adding ".Dump();" to dump the result of an expression
        code =>
        {
            var trimmed = code.TrimEnd();
            if (!trimmed.EndsWith(";") && !trimmed.EndsWith(".Dump()"))
            {
                return (true, $"({trimmed}).Dump();");
            }

            return (false, code);
        },

        // Try adding ";" to execute an expression
        code =>
        {
            var trimmed = code.TrimEnd();
            return !trimmed.EndsWith(";")
                ? (true, trimmed + ";")
                : (false, code);
        }
    ];

    public async Task<ParseAndCompileResult?> ParseAndCompileAsync(
        string code,
        Script script,
        CancellationToken cancellationToken)
    {
        var dependencies = await scriptDependencyResolver.GetDependenciesAsync(script, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var compileAssemblyImageDeps = dependencies.References
            .Select(d =>
                d.Dependant != Dependant.ScriptHost && d.Reference is AssemblyImageReference air
                    ? air.AssemblyImage
                    : null!)
            .Where(x => x != null!)
            .ToArray();

        var compileAssemblyFileDeps = dependencies.References
            .Where(x => x.Dependant != Dependant.ScriptHost)
            .SelectMany(x => x.Assets)
            .DistinctBy(x => x.Path)
            .Where(x => x.IsManagedAssembly)
            .Select(x => new
            {
                x.Path,
                AssemblyName = AssemblyName.GetAssemblyName(x.Path)
            })
            // Choose the highest version of duplicate assemblies
            .GroupBy(a => a.AssemblyName.Name)
            .Select(grp => grp.OrderBy(x => x.AssemblyName.Version).Last())
            .Select(x => x.Path)
            .ToHashSet();

        return TryPermutations(
            code,
            script,
            compileAssemblyImageDeps,
            compileAssemblyFileDeps,
            new SourceCodeCollection(dependencies.Code.SelectMany(x => x.Code)),
            cancellationToken);
    }

    private ParseAndCompileResult? TryPermutations(string code,
        Script script,
        IList<AssemblyImage> referenceAssemblyImages,
        HashSet<string> referenceAssemblyPaths,
        SourceCodeCollection additionalCode,
        CancellationToken cancellationToken)
    {
        ParseAndCompileResult? firstResult = null;

        foreach (var permutation in _permutations)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var pResult = permutation(code);

            if (!pResult.shouldAttempt)
            {
                continue;
            }

            var result = ParseAndCompilePermutation(
                pResult.code,
                script,
                referenceAssemblyImages,
                referenceAssemblyPaths,
                additionalCode,
                cancellationToken);

            if (result?.CompilationResult.Success == true)
            {
                return result;
            }

            // Save the first one
            firstResult ??= result;
        }

        // If we got here none of the permutations were compilable, return the compilation
        // result of the first permutation
        return firstResult;
    }

    private ParseAndCompileResult? ParseAndCompilePermutation(
        string code,
        Script script,
        IList<AssemblyImage> referenceAssemblyImages,
        HashSet<string> referenceAssemblyPaths,
        SourceCodeCollection additionalCode,
        CancellationToken cancellationToken)
    {
        var parsingResult = codeParser.Parse(
            code,
            script.Config.Kind,
            script.Config.Namespaces,
            new CodeParsingOptions
            {
                IncludeAspNetUsings = script.Config.UseAspNet,
                AdditionalCode = additionalCode
            });

        parsingResult.BootstrapperProgram.Code.Update(parsingResult.BootstrapperProgram.Code.Value?
            .Replace("SCRIPT_ID", script.Id.ToString())
            .Replace("SCRIPT_NAME", script.Name)
            .Replace("SCRIPT_LOCATION", script.Path));

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var fullProgram = parsingResult.GetFullProgram();

        var compilationInput = new CompilationInput(
                fullProgram.ToCodeString(),
                script.Config.TargetFrameworkVersion,
                referenceAssemblyImages.Select(a => a.Image).ToHashSet(),
                referenceAssemblyPaths)
            .WithOptimizationLevel(script.Config.OptimizationLevel)
            .WithUseAspNet(script.Config.UseAspNet);

        var compilationResult = codeCompiler.Compile(compilationInput);

        return new ParseAndCompileResult(parsingResult, compilationResult);
    }
}
