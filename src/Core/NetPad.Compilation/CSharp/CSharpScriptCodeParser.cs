using System;
using System.Collections.Generic;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Compilation.CSharp
{
    public class CSharpScriptCodeParser : ICodeParser
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
                .Replace("Console.WriteLine", "OutputWriteLine")
                .Replace("Console.Write", "OutputWrite");

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

            if (script.Config.Kind == ScriptKind.Expression || script.Config.Kind == ScriptKind.Statements)
            {
                userCode = scriptCode;
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

private static IScriptRuntimeOutputWriter StaticOutputWriter;
StaticOutputWriter = OutputWriter;

private static void OutputWrite(object? o)
{{
    StaticOutputWriter.WriteAsync(o?.ToString());
}}

private static void OutputWriteLine(object? o)
{{
    StaticOutputWriter.WriteAsync(o?.ToString() + ""\n"");
}}

public static T? Dump<T>(this T? o)
{{
    OutputWriteLine(o);
    return o;
}}

{1}
";
        }

        public string GetUserProgramTemplate()
        {
            return "{0}";
        }
    }
}
