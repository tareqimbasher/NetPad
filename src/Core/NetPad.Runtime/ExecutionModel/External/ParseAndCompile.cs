using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

internal record ParseAndCompileResult(CodeParsingResult ParsingResult, CompilationResult CompilationResult);

internal static class ParseAndCompile
{
    // We will try different permutations of the user's code, starting with running it as-is. The idea is to account
    // for, and give the ability for users to, run expressions not ending with semi-colon or .Dump() and generate
    // the "missing" pieces to run the expression.
    // TODO use SyntaxTree to determine if expression or not. Currently not a performance bottleneck however.
    private static readonly List<Func<string, (bool shouldAttempt, string code)>> _permutations =
    [
        code => (true, code),

        // Try adding ".Dump();" to dump the result of an expression
        code =>
        {
            var fixedCode = code.Trim();

            if (!fixedCode.EndsWith(";") && !fixedCode.EndsWith(".Dump()"))
            {
                fixedCode = $"({fixedCode}).Dump();";
                return (true, fixedCode);
            }

            return (false, code);
        },

        // Try adding ";" to execute an expression
        code =>
        {
            var trimmedCode = code.Trim();
            return !trimmedCode.EndsWith(";")
                ? (true, trimmedCode + ";")
                : (false, code);
        }
    ];

    public static ParseAndCompileResult Do(
        string code,
        Script script,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        List<AssemblyImage> referenceAssemblyImages,
        HashSet<string> referenceAssemblyPaths,
        SourceCodeCollection additionalCode)
    {
        ParseAndCompileResult? asIsResult = null;

        for (var ixPerm = 0; ixPerm < _permutations.Count; ixPerm++)
        {
            var permutationFunc = _permutations[ixPerm];
            var permutation = permutationFunc(code);

            if (!permutation.shouldAttempt)
            {
                continue;
            }

            var result = ParseAndCompilePermutation(
                permutation.code,
                script,
                codeParser,
                codeCompiler,
                referenceAssemblyImages,
                referenceAssemblyPaths,
                additionalCode);

            if (result.CompilationResult.Success)
            {
                return result;
            }

            if (ixPerm == 0)
            {
                asIsResult = result;
            }
        }

        // If we got here, none of the permutations were compilable
        return asIsResult!;
    }

    private static ParseAndCompileResult ParseAndCompilePermutation(
        string code,
        Script script,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        List<AssemblyImage> referenceAssemblyImages,
        HashSet<string> referenceAssemblyPaths,
        SourceCodeCollection additionalCode)
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
