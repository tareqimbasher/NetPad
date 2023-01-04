using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using NetPad.Compilation.CSharp;
using NetPad.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Compilation.Tests.CSharp;

public class CSharpCodeCompilerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CSharpCodeCompilerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Can_Compile_EmptyProgram()
    {
        var code = GetProgram("");

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Fact]
    public void Can_Compile_SimpleProgram()
    {
        var code = GetProgram("Console.WriteLine(\"Hello World\");");

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Theory]
    [InlineData("Console.Write(\"Hello World\";")]
    [InlineData("Console.Write(\"Hello World\")")]
    [InlineData("Console.(\"Hello World\")")]
    [InlineData("Console.WriteE(\"Hello World\")")]
    [InlineData("Console.write(\"Hello World\");")]
    [InlineData("foobar")]
    [InlineData("var date = DateTime.Utcnow;")]
    public void Fails_On_Syntax_Error(string code)
    {
        code = GetProgram(code);

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.False(result.Success);
    }

    [Fact]
    public void Returns_Diagnostics_On_Compilation_Error()
    {
        var code = GetProgram("foobar");

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Theory]
    [InlineData("System.Threading.Tasks", "var task = Task.CompletedTask;")]
    [InlineData("System.Collections.Generic", "var list = new List<string>();")]
    public void Can_Compile_Program_With_DotNet_Usings(string @namespace, string code)
    {
        code = GetProgram(code, @namespace);

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Fact]
    public void Compiler_Uses_CSharp9_Features()
    {
        var compiler = new CSharpCodeCompiler();

        CSharpParseOptions parseOptions = compiler.GetParseOptions();

        Assert.Equal(LanguageVersion.CSharp10, parseOptions.LanguageVersion);
    }

    [Fact]
    public void Can_Compile_CSharp8_Features()
    {
        var code = GetProgram("using var stream = new MemoryStream();", "System.IO");

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Fact]
    public void Can_Compile_CSharp9_Features()
    {
        var code = GetProgram("DateTime datetime = new();");

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Fact]
    public void Can_Compile_CSharp10_Features()
    {
        var code = GetProgram("var point = (1, 2); int x = 0; (x, int y) = point;");

        var result = new CSharpCodeCompiler().Compile(new CompilationInput(code));

        Assert.True(result.Success);
    }

    private string GetProgram(string code, params string[] additionalNamespaces)
    {
        var namespaces = new[] { "System" }.Union(additionalNamespaces)
            .Select(ns => $"using {ns};");

        var usings = string.Join("\n", namespaces);

        return $@"
{usings}
public class Program
{{
    public static void Main()
    {{
        {code}
    }}
}}
";
    }
}
