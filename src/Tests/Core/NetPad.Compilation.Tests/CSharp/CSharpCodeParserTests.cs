using System;
using System.Linq;
using NetPad.Compilation.CSharp;
using NetPad.Scripts;
using NetPad.Utilities;
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
        public void GetNamespaces_Adds_Namespaces_Needed_By_BaseProgram()
        {
            var script = GetScript();
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script);

            Assert.Equal(namespaces, CSharpCodeParser.NamespacesNeededByBaseProgram);
        }

        [Fact]
        public void GetNamespaces_Returns_Script_Namespaces()
        {
            var scriptNamespaces = new[]
            {
                "ScriptNamespace1",
                "ScriptNamespace2"
            };
            var script = GetScript();
            script.Config.SetNamespaces(scriptNamespaces);
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script)
                .Except(CSharpCodeParser.NamespacesNeededByBaseProgram);

            Assert.Equal(scriptNamespaces, namespaces);
        }

        [Fact]
        public void GetNamespaces_Returns_Additional_Passed_Namespaces()
        {
            var additionalNamespaces = new[]
            {
                "AdditionalNamespace1",
                "AdditionalNamespace2"
            };
            var script = GetScript();
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script, additionalNamespaces)
                .Except(CSharpCodeParser.NamespacesNeededByBaseProgram);

            Assert.Equal(additionalNamespaces, namespaces);
        }

        [Fact]
        public void GetNamespaces_Returns_Script_And_Additional_Passed_Namespaces()
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
            script.Config.SetNamespaces(scriptNamespaces);
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script, additionalNamespaces)
                .Except(CSharpCodeParser.NamespacesNeededByBaseProgram);

            Assert.Equal(scriptNamespaces.Union(additionalNamespaces), namespaces);
        }

        [Fact]
        public void GetNamespaces_Returns_Distinct_Namespaces()
        {
            var additionalNamespaces = new[]
            {
                "AdditionalNamespace1",
                "AdditionalNamespace1"
            };
            var script = GetScript();
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script, additionalNamespaces)
                .Except(CSharpCodeParser.NamespacesNeededByBaseProgram);

            Assert.Equal(new[] { "AdditionalNamespace1" }, namespaces);
        }

        [Fact]
        public void GetNamespaces_Guards_Against_Null_Additional_Namespaces_Param()
        {
            string[]? additionalNamespaces = null;
            var script = GetScript();
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script, additionalNamespaces!)
                .Except(CSharpCodeParser.NamespacesNeededByBaseProgram);

            Assert.Empty(namespaces);
        }

        [Fact]
        public void GetNamespaces_Filters_Out_Null_Or_Whitespace_Namespaces()
        {
            var scriptNamespaces = new[]
            {
                "ScriptNamespace1",
                null
            };

            var additionalNamespaces = new[]
            {
                "AdditionalNamespace1",
                ""
            };
            var script = GetScript();
            script.Config.SetNamespaces(scriptNamespaces!);
            var parser = new CSharpCodeParser();

            var namespaces = parser.GetNamespaces(script, additionalNamespaces)
                .Except(CSharpCodeParser.NamespacesNeededByBaseProgram);

            Assert.Equal(new[]
            {
                "ScriptNamespace1",
                "AdditionalNamespace1"
            }, namespaces);
        }

        [Fact]
        public void Parsed_Code_Includes_Namespaces()
        {
            var scriptNamespaces = new[]
            {
                "ScriptNamespace1",
                "ScriptNamespace2"
            };

            var script = GetScript();
            script.Config.SetNamespaces(scriptNamespaces);
            var parser = new CSharpCodeParser();

            var result = parser.Parse(script);

            foreach (var @namespace in scriptNamespaces)
            {
                Assert.Contains(@namespace, result.Namespaces);
            }
        }

        [Fact]
        public void BaseProgramTemplate_Has_Correct_Class_Declaration()
        {
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(GetScript());

            Assert.Contains($"class {CSharpCodeParser.BootstrapperClassName}", parsingResult.BootstrapperProgram);
        }

        [Fact]
        public void BaseProgramTemplate_Has_Correct_SetIO_Method()
        {
            var parser = new CSharpCodeParser();

            var parsingResult = parser.Parse(GetScript());

            Assert.Contains(CSharpCodeParser.BootstrapperSetIOMethodName, parsingResult.BootstrapperProgram);
        }

        [Fact]
        public void GetUserCode_Throws_NotImplementedException_For_Expression_Type_Script()
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Expression);
            script.UpdateCode("DateTime.Now");
            var parser = new CSharpCodeParser();

            Assert.Throws<NotImplementedException>(() => parser.GetUserCode(script.Code, script.Config.Kind));
        }

        private Script GetScript() => new Script("Test Script");
    }
}
