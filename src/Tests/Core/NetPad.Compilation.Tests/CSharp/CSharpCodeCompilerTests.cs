using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NetPad.Compilation.CSharp;
using NetPad.Configuration;
using NetPad.DotNet;
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

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Fact]
    public void Can_Compile_SimpleProgram()
    {
        var code = GetProgram("Console.WriteLine(\"Hello World\");");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

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

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.False(result.Success);
    }

    [Fact]
    public void Returns_Diagnostics_On_Compilation_Error()
    {
        var code = GetProgram("foobar");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Theory]
    [InlineData("System.Threading.Tasks", "var task = Task.CompletedTask;")]
    [InlineData("System.Collections.Generic", "var list = new List<string>();")]
    public void Can_Compile_Program_With_DotNet_Usings(string @namespace, string code)
    {
        code = GetProgram(code, @namespace);

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.True(result.Success, result.Diagnostics.Select(d => d.GetMessage()).JoinToString("|"));
    }

    [Theory]
    [InlineData(DotNetFrameworkVersion.DotNet2, null)]
    [InlineData(DotNetFrameworkVersion.DotNet3, null)]
    [InlineData(DotNetFrameworkVersion.DotNet5, null)]
    [InlineData(DotNetFrameworkVersion.DotNet6, LanguageVersion.CSharp10)]
    [InlineData(DotNetFrameworkVersion.DotNet7, LanguageVersion.CSharp11)]
    [InlineData(DotNetFrameworkVersion.DotNet8, LanguageVersion.CSharp12)]
    public void Compiler_Uses_Correct_CSharp_LanguageVersion(DotNetFrameworkVersion targetFrameworkVersion, LanguageVersion? expectedLangVersion)
    {
        var compiler = CreateCSharpCodeCompiler();

        if (expectedLangVersion == null)
        {
            Assert.ThrowsAny<Exception>(() => compiler.GetParseOptions(targetFrameworkVersion, OptimizationLevel.Debug));
        }
        else
        {
            CSharpParseOptions parseOptions = compiler.GetParseOptions(targetFrameworkVersion, OptimizationLevel.Debug);
            Assert.Equal(expectedLangVersion, parseOptions.LanguageVersion);
        }
    }

    [Fact]
    public void Can_Compile_CSharp8_Features()
    {
        var code = GetProgram("using var stream = new MemoryStream();", "System.IO");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.True(result.Success, result.Diagnostics.JoinToString(Environment.NewLine));
    }

    [Fact]
    public void Can_Compile_CSharp9_Features()
    {
        var code = GetProgram("DateTime datetime = new();");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.True(result.Success, result.Diagnostics.JoinToString(Environment.NewLine));
    }

    [Fact]
    public void Can_Compile_CSharp10_Features()
    {
        var code = GetProgram("var point = (1, 2); int x = 0; (x, int y) = point;");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet6));

        Assert.True(result.Success, result.Diagnostics.JoinToString(Environment.NewLine));
    }

    [Fact]
    public void Can_Compile_CSharp11_Features()
    {
        var code = GetProgram("var str = \"\"\"\nsome text\n\"\"\";");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet7));

        Assert.True(result.Success, result.Diagnostics.JoinToString(Environment.NewLine));
    }

    [Fact]
    public void Can_Compile_CSharp12_Features()
    {
        var code = GetProgram("var IncrementBy = (int source, int increment = 1) => source + increment;");

        var result = CreateCSharpCodeCompiler().Compile(new CompilationInput(code, DotNetFrameworkVersion.DotNet8));

        Assert.True(result.Success, result.Diagnostics.JoinToString(Environment.NewLine));
    }

    private CSharpCodeCompiler CreateCSharpCodeCompiler()
    {
        return new CSharpCodeCompiler(new DotNetInfo(new Settings()));
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
