using NetPad.Common;
using NetPad.Compilation;
using NetPad.DotNet.CodeAnalysis;
using NetPad.ExecutionModel.External;
using NetPad.Scripts;
using Xunit;

namespace NetPad.Runtime.Tests.ExecutionModel.External;

public class ExternalRunnerCSharpCodeParserTests
{
    [Fact]
    public void ParsedResult_Contains_Script_Namespaces()
    {
        var scriptNamespaces = new[]
        {
            "ScriptNamespace1",
            "ScriptNamespace2"
        };
        var script = GetScript();
        script.Config.SetNamespaces(scriptNamespaces);
        var parser = new ExternalRunnerCSharpCodeParser();

        var parsingResult = parser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces);
        var parsedUsings = parsingResult.UserProgram.Usings.Select(u => u.Value);

        Assert.Equal(scriptNamespaces, parsedUsings);
    }

    [Fact]
    public void ParsedResult_Contains_AdditionalCode_Namespaces()
    {
        var additionalNamespaces = new[]
        {
            "AdditionalNamespace1",
            "AdditionalNamespace2"
        };
        var parseOptions = new CodeParsingOptions();
        parseOptions.AdditionalCode.Add(new SourceCode(additionalNamespaces));
        var script = GetScript();
        var parser = new ExternalRunnerCSharpCodeParser();

        var parsingResult = parser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces, parseOptions);
        var parsedUsings = parsingResult.AdditionalCodeProgram?.GetAllUsings().Select(u => u.Value);

        Assert.NotNull(parsedUsings);
        Assert.Equal(additionalNamespaces, parsedUsings);
    }

    [Fact]
    public void ParsedResult_Contains_Script_And_AdditionalCode_Namespaces()
    {
        var scriptNamespaces = new[]
        {
            "ScriptNamespace1",
            "ScriptNamespace2"
        };

        var additionalNamespaces = new[]
        {
            "AdditionalNamespace1",
            "AdditionalNamespace2"
        };
        var script = GetScript();
        var parseOptions = new CodeParsingOptions();
        parseOptions.AdditionalCode.Add(new SourceCode(additionalNamespaces));
        script.Config.SetNamespaces(scriptNamespaces);
        var parser = new ExternalRunnerCSharpCodeParser();

        var parsingResult = parser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces, parseOptions);
        var parsedUserProgramUsings = parsingResult.UserProgram.Usings.Select(u => u.Value);
        var parsedAdditionalCodeUsings = parsingResult.AdditionalCodeProgram?.GetAllUsings().Select(u => u.Value);

        Assert.NotNull(parsedAdditionalCodeUsings);
        Assert.Equal(
            scriptNamespaces.Union(additionalNamespaces),
            parsedUserProgramUsings.Union(parsedAdditionalCodeUsings));
    }

    [Fact]
    public void EmbeddedBootstrapperProgramIsParsable()
    {
        var bootstrapperProgram = ExternalRunnerCSharpCodeParser.GetEmbeddedBootstrapperProgram();

        SourceCode.Parse(bootstrapperProgram);
    }

    [Fact]
    public void GetEmbeddedSqlProgramIsParsable()
    {
        var bootstrapperProgram = ExternalRunnerCSharpCodeParser.GetEmbeddedSqlProgram();

        SourceCode.Parse(bootstrapperProgram);
    }

    private Script GetScript() => new(Guid.NewGuid(), "Test Script", new ScriptConfig(ScriptKind.Program, GlobalConsts.AppDotNetFrameworkVersion));
}
