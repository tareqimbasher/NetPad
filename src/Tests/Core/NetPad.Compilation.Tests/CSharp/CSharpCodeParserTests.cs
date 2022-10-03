using System;
using System.Linq;
using NetPad.Compilation.CSharp;
using NetPad.DotNet;
using NetPad.Scripts;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Compilation.Tests.CSharp
{
    public class CSharpCodeParserTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CSharpCodeParserTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(script);

            Assert.Equal(scriptNamespaces, parsingResult.UserProgram.Usings.Select(u => u.Value));
        }

        [Fact]
        public void ParsedResult_Contains_Additional_Passed_Namespaces()
        {
            var additionalNamespaces = new[]
            {
                "AdditionalNamespace1",
                "AdditionalNamespace2"
            };
            var parseOptions = new CodeParsingOptions();
            parseOptions.AdditionalCode.Add(new SourceCode(additionalNamespaces));
            var script = GetScript();
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(script, parseOptions);

            Assert.Equal(additionalNamespaces, parsingResult.AdditionalCodeProgram?.GetAllUsings().Select(u => u.Value));
        }

        [Fact]
        public void ParsedResult_Contains_Script_And_Additional_Passed_Namespaces()
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
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(script, parseOptions);

            Assert.Equal(
                scriptNamespaces.Union(additionalNamespaces),
                parsingResult.UserProgram.Usings.Select(u => u.Value)
                    .Union(parseOptions.AdditionalCode!.GetAllUsings().Select(u => u.Value)));
        }

        [Fact]
        public void BaseProgramTemplate_Has_Correct_Class_Declaration()
        {
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(GetScript());

            Assert.Contains($"class {CSharpCodeParser.BootstrapperClassName}", parsingResult.BootstrapperProgram.Code.Value!);
        }

        [Fact]
        public void BaseProgramTemplate_Has_Correct_SetIO_Method()
        {
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(GetScript());

            Assert.Contains(CSharpCodeParser.BootstrapperSetIOMethodName, parsingResult.BootstrapperProgram.Code.Value!);
        }

        [Fact]
        public void GetUserCode_Throws_NotImplementedException_For_Expression_Type_Script()
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Expression);
            script.UpdateCode("DateTime.Now");
            var parser = new CSharpCodeParser();

            Assert.Throws<NotImplementedException>(() => parser.GetUserProgram(script.Code, script.Config.Kind));
        }

        private Script GetScript() => new Script("Test Script");
    }
}
