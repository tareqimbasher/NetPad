using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Compilation.CSharp
{
    public class CSharpCodeParser : ICodeParser
    {
        public static readonly string[] NamespacesNeededByBaseProgram =
        {
            "System",
            "System.Threading.Tasks",
            "NetPad.IO"
        };

        public CodeParsingResult Parse(Script script, params string[] additionalNamespaces)
        {
            var namespaces = GetNamespaces(script, additionalNamespaces);
            var usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));

            var userCode = GetUserCode(script);
            var userProgramTemplate = GetUserProgramTemplate();
            var userProgram = string.Format(userProgramTemplate, userCode)
                // TODO implementation should be improved to use syntax tree instead of a simple string replace
                .Replace("Console.WriteLine", "Program.OutputWriteLine")
                .Replace("Console.Write", "Program.OutputWrite");

            var baseProgramTemplate = GetBaseProgramTemplate();
            var fullProgram = string.Format(baseProgramTemplate, usings, userProgram);

            int userCodeStartLine = 0;
            using var reader = new StringReader(fullProgram);
            string? line = "";
            int lineIndex = -1;
            while ((line = reader.ReadLine()) != null)
            {
                lineIndex++;
                if (line.Contains("// USER CODE STARTS HERE"))
                {
                    userCodeStartLine = lineIndex + 1;
                    break;
                }
            }

            return new CodeParsingResult(fullProgram, userCodeStartLine)
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
            string scriptCode = "// USER CODE STARTS HERE\n" + script.Code;

            if (script.Config.Kind == ScriptKind.Expression)
            {
                throw new NotImplementedException("Expression code parsing is not implemented yet.");
            }
            else if (script.Config.Kind == ScriptKind.Statements)
            {
                userCode = $@"public async Task Main()
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
            return @"{0}

public class Program
{{
    private static IOutputWriter OutputWriter {{ get; set; }}
    private static Exception? Exception {{ get; set; }}

    // Entry point used when running script in external process
    static async Task Main(string[] args)
    {{
        await Start(new ActionOutputWriter((o, t) => Console.WriteLine(o?.ToString())));
    }}

    private static async Task Start(IOutputWriter outputWriter)
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

    public static void OutputWrite(object? o = null, string? title = null)
    {{
        OutputWriter.WriteAsync(o, title);
    }}

    public static void OutputWriteLine(object? o = null, string? title = null)
    {{
        OutputWriter.WriteAsync(o, title);
    }}
}}

public static class Exts
{{
        /// <summary>
        /// Dumps this object to the results view.
        /// </summary>
        /// <returns>The object being dumped.</returns>
    public static T? Dump<T>(this T? o, string? title = null)
    {{
        Program.OutputWriteLine(o, title);
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
