using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Moq;
using NetPad.Compilation;
using NetPad.Compilation.Scripts;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.DotNet.CodeAnalysis;
using NetPad.Scripts;

namespace NetPad.Runtime.Tests.Compilation.Scripts;

public class ScriptCompilerTests
{
    private readonly Mock<IScriptDependencyResolver> _dependencyResolver;
    private readonly Mock<ICodeParser> _codeParser;
    private readonly Mock<ICodeCompiler> _codeCompiler;
    private readonly List<string> _compiledCodeInputs;

    public ScriptCompilerTests()
    {
        _dependencyResolver = new Mock<IScriptDependencyResolver>();
        _dependencyResolver
            .Setup(r => r.GetDependenciesAsync(It.IsAny<Script>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScriptDependencies([], []));

        _codeParser = new Mock<ICodeParser>();
        _codeParser
            .Setup(p => p.Parse(It.IsAny<Script>(), It.IsAny<string?>(), It.IsAny<CodeParsingOptions?>()))
            .Returns((Script _, string? code, CodeParsingOptions? _) =>
            {
                var userProgram = new SourceCode(new Code(code));
                var bootstrapper = new SourceCode(new Code("// bootstrapper with SCRIPT_ID SCRIPT_NAME SCRIPT_LOCATION"));
                return new CodeParsingResult(userProgram, bootstrapper, null);
            });

        _compiledCodeInputs = [];
        _codeCompiler = new Mock<ICodeCompiler>();
    }

    private ScriptCompiler CreateCompiler()
    {
        return new ScriptCompiler(_dependencyResolver.Object, _codeParser.Object, _codeCompiler.Object);
    }

    private void SetupCompilerReturns(params bool[] successPerCall)
    {
        var callIndex = 0;
        _codeCompiler
            .Setup(c => c.Compile(It.IsAny<CompilationInput>()))
            .Returns((CompilationInput input) =>
            {
                _compiledCodeInputs.Add(input.Code);
                var success = callIndex < successPerCall.Length && successPerCall[callIndex];
                callIndex++;
                return new CompilationResult(
                    success,
                    new AssemblyName("Test"),
                    "Test.dll",
                    success ? [0x00] : [],
                    ImmutableArray<Diagnostic>.Empty);
            });
    }

    [Fact]
    public async Task FirstPermutation_AsIs_WhenCompilationSucceeds()
    {
        SetupCompilerReturns(true);
        var script = ScriptTestHelper.CreateScript();
        script.UpdateCode("Console.WriteLine(42);");

        var result = await CreateCompiler().ParseAndCompileAsync("Console.WriteLine(42);", script, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.CompilationResult.Success);
        // Should only compile once (the as-is code)
        Assert.Single(_compiledCodeInputs);
    }

    [Fact]
    public async Task SecondPermutation_AddsDump_WhenFirstFails()
    {
        // First attempt (as-is) fails, second (with .Dump()) succeeds
        SetupCompilerReturns(false, true);
        var script = ScriptTestHelper.CreateScript();
        var code = "42";

        var result = await CreateCompiler().ParseAndCompileAsync(code, script, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.CompilationResult.Success);
        Assert.Equal(2, _compiledCodeInputs.Count);
    }

    [Fact]
    public async Task ThirdPermutation_AddsSemicolon_WhenFirstTwoFail()
    {
        // All three attempts: as-is fails, .Dump() fails, semicolon succeeds
        SetupCompilerReturns(false, false, true);
        var script = ScriptTestHelper.CreateScript();
        var code = "42";

        var result = await CreateCompiler().ParseAndCompileAsync(code, script, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.CompilationResult.Success);
        Assert.Equal(3, _compiledCodeInputs.Count);
    }

    [Fact]
    public async Task AllPermutationsFail_ReturnsFirstPermutationResult()
    {
        SetupCompilerReturns(false, false, false);
        var script = ScriptTestHelper.CreateScript();
        var code = "invalid code";

        var result = await CreateCompiler().ParseAndCompileAsync(code, script, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.CompilationResult.Success);
    }

    [Fact]
    public async Task CodeEndingWithSemicolon_SkipsDumpAndSemicolonPermutations()
    {
        // Code already ends with ";", so .Dump() and ";" permutations should be skipped
        SetupCompilerReturns(false);
        var script = ScriptTestHelper.CreateScript();
        var code = "var x = 1;";

        var result = await CreateCompiler().ParseAndCompileAsync(code, script, CancellationToken.None);

        Assert.NotNull(result);
        // Only the as-is attempt should have been tried
        Assert.Single(_compiledCodeInputs);
    }

    [Fact]
    public async Task CodeEndingWithDump_SkipsDumpPermutation()
    {
        // Code already ends with ".Dump()", so .Dump() permutation is skipped, but ";" is tried
        SetupCompilerReturns(false, false);
        var script = ScriptTestHelper.CreateScript();
        var code = "myVar.Dump()";

        var result = await CreateCompiler().ParseAndCompileAsync(code, script, CancellationToken.None);

        Assert.NotNull(result);
        // As-is + semicolon (Dump permutation skipped since it already ends with .Dump())
        Assert.Equal(2, _compiledCodeInputs.Count);
    }

    [Fact]
    public async Task Cancellation_ReturnsNull()
    {
        var script = ScriptTestHelper.CreateScript();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await CreateCompiler().ParseAndCompileAsync("code", script, cts.Token);

        Assert.Null(result);
    }

}
