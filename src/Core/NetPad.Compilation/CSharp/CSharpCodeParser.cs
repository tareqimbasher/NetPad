using System;
using System.Collections.Generic;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Compilation.CSharp
{
    public class CSharpCodeParser : ICodeParser
    {
        public static readonly string[] NamespacesNeededByBaseProgram = new[]
        {
            "System",
            "System.Threading.Tasks",
            "NetPad.Runtimes"
        };

        public CodeParsingResult Parse(Script script, params string[] additionalNamespaces)
        {
            var namespaces = GetNamespaces(script, additionalNamespaces);
            var usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));

            var userCode = GetUserCode(script);
            var userProgramTemplate = GetUserProgramTemplate();
            var userProgram = string.Format(userProgramTemplate, userCode)
                // TODO implementation should be improved to use syntax tree instead of a simple string replace
                .Replace("Console.WriteLine", "UserScript.OutputWriteLine")
                .Replace("Console.Write", "UserScript.OutputWrite");

            var baseProgramTemplate = GetBaseProgramTemplate();
            var program = string.Format(baseProgramTemplate, usings, userProgram);

            return new CodeParsingResult(program)
            {
                Namespaces = namespaces.ToList(),
                UserProgram = userProgram
            };
        }

        public IEnumerable<string> GetNamespaces(Script script, params string[] additionalNamespaces)
        {
            additionalNamespaces ??= Array.Empty<string>();

            return NamespacesNeededByBaseProgram
                .Union(script.Config.Namespaces.Where(ns => !string.IsNullOrWhiteSpace(ns)))
                .Union(additionalNamespaces.Where(ns => !string.IsNullOrWhiteSpace(ns)))
                .Distinct();
        }

        public string GetUserCode(Script script)
        {
            string userCode;
            string scriptCode = script.Code;

            if (script.Config.Kind == ScriptKind.Expression)
            {
                throw new NotImplementedException("Expression code parsing is not implemented yet.");
            }
            else if (script.Config.Kind == ScriptKind.Statements)
            {
                userCode = $@"
public async Task Main()
{{
    {scriptCode}
}}
";
            }
            else if (script.Config.Kind == ScriptKind.Program)
            {
                userCode = scriptCode;
            }
            else
            {
                throw new Exception($"Unrecognized script kind: {script.Config.Kind}");
            }

            return userCode;
        }

        public string GetBaseProgramTemplate()
        {
            return @"
{0}

public static class UserScript
{{
    private static IScriptRuntimeOutputWriter OutputWriter {{ get; set; }}
    private static Exception? Exception {{ get; set; }}

    private static async Task Main(IScriptRuntimeOutputWriter outputWriter)
    {{
        OutputWriter = outputWriter;

        try
        {{
            await new UserScript_Program().Main();
        }}
        catch (Exception ex)
        {{
            Exception = ex;
        }}
    }}

    public static void OutputWrite(object? o)
    {{
        OutputWriter.WriteAsync(o?.ToString());
    }}

    public static void OutputWriteLine(object? o)
    {{
        OutputWriter.WriteAsync(o?.ToString() + ""\n"");
    }}

    public static T? Dump<T>(this T? o)
    {{
        OutputWriteLine(o);
        return o;
    }}
}}

{1}
";
        }

        public string GetUserProgramTemplate()
        {
            return @"
public class UserScript_Program
{{
    {0}
}}
";
        }
    }
}
