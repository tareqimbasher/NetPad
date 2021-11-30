using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace NetPad.Compilation.Tests.CSharp
{
    public class ScriptingParsingTests
    {
        [Fact]
        public void Can_Compile_SimpleProgram2()
        {
            var codes = new[]
            {
                ("AdditionExpression", "2+4"),
                ("AdditionStatement", "2+4;"),
                ("WriteLineExpression", "Console.Write(\"Hello World\")"),
                ("WriteLineStatement", "Console.Write(\"Hello World\");"),
                ("WriteLineStatement2", "var s = Console.WriteL(\"Hello World\");"),
            };

            foreach (var code in codes)
            {
                SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(code.Item2);
                var root = syntaxTree.GetCompilationUnitRoot();
                var m = root.Members[0] as GlobalStatementSyntax;
                var e = m.Statement.GetType();
                var s = m.Statement;

                // var d = syntaxTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                //
                // if (d.Any())
                //     ;

                // File.WriteAllText($"/home/tips/X/tmp/Test/{code.Item1}.json", JsonConvert.SerializeObject(root, new JsonSerializerSettings()
                // {
                //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                //     Formatting = Formatting.Indented,
                //     TypeNameHandling = TypeNameHandling.All
                // }));
            }
        }
    }
}
