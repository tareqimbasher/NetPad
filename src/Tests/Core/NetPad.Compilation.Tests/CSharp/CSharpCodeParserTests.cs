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
                Assert.Contains($"using {@namespace};", result.FullProgram);
            }
        }

        [Fact]
        public void BaseProgramTemplate_Has_Correct_Class_Declaration()
        {
            var parser = new CSharpCodeParser();

            var baseProgramTemplate = parser.GetBaseProgramTemplate();

            Assert.Contains("public class Program", baseProgramTemplate);
        }

        [Fact]
        public void UserProgramTemplate_Has_Correct_Class_Declaration()
        {
            var parser = new CSharpCodeParser();

            var userProgramTemplate = parser.GetUserProgramTemplate();

            Assert.Contains("public class UserScript_Program", userProgramTemplate);
        }

        [Fact]
        public void GetUserCode_Throws_NotImplementedException_For_Expression_Type_Script()
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Expression);
            script.UpdateCode("DateTime.Now");
            var parser = new CSharpCodeParser();

            Assert.Throws<NotImplementedException>(() => parser.GetUserCode(script));
        }

        [Fact]
        public void GetUserCode_Returns_Code_Wrapped_In_Main_Method_For_Statements_Type_Script()
        {
            var script = GetScript();
            script.Config.SetKind(ScriptKind.Statements);
            script.UpdateCode("Console.WriteLine(DateTime.Now);");
            var parser = new CSharpCodeParser();

            var userCode = parser.GetUserCode(script);

            var expected = @"public async Task Main()
{
Console.WriteLine(DateTime.Now);
}
";

            userCode = userCode.Split("\n")
                .Where(l => !l.Trim().StartsWith("//"))
                .JoinToString("\n");

            Assert.Equal(expected, userCode);
        }

        [Fact]
        public void Parsed_Code_Replaces_ConsoleWrite_With_UserScript_OutputWrite()
        {
            var script = GetScript();
            script.UpdateCode("Statement 1; Console.Write(\"Some Text\");");
            var parser = new CSharpCodeParser();

            var result = parser.Parse(script);

            Assert.DoesNotContain("Console.Write(\"Some Text\");", result.UserProgram!);
            Assert.Contains("Program.OutputWrite(\"Some Text\");", result.UserProgram!);
        }

        [Fact]
        public void Parsed_Code_Replaces_ConsoleWriteLine_With_UserScript_OutputWriteLine()
        {
            var script = GetScript();
            script.UpdateCode("Statement 1; Console.WriteLine(\"Some Text\");");
            var parser = new CSharpCodeParser();

            var result = parser.Parse(script);

            Assert.DoesNotContain("Console.WriteLine(\"Some Text\");", result.UserProgram!);
            Assert.Contains("Program.OutputWriteLine(\"Some Text\");", result.UserProgram!);
        }

        private Script GetScript() => new Script("Test Script");
    }
}
