using System;
using System.Collections.Generic;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Runtimes
{
    public class CodeParser
    {
        public static string GetScriptCode(Script script)
        {
            string scriptCode = script.Code;
            string code;

            var namespaces = new List<string>
            {
                "NetPad.Runtimes"
            };

            namespaces = namespaces.Union(script.Config.Namespaces).Distinct().ToList();

            var usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));

            const string program = @"
{1}

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

    public static void ConsoleWrite(object? o)
    {{
        OutputWriter.WriteAsync(o?.ToString());
    }}

    public static void ConsoleWriteLine(object? o)
    {{
        OutputWriter.WriteAsync(o?.ToString() + ""\n"");
    }}
}}

public class UserScript_Program
{{
    {0}
}}
";

            if (script.Config.Kind == ScriptKind.Expression)
            {
                throw new NotImplementedException("Expression code parsing is not implemented yet.");
            }
            else if (script.Config.Kind == ScriptKind.Statements)
            {
                code = $@"
public async Task Main()
{{
    {scriptCode}
}}
";
            }
            else if (script.Config.Kind == ScriptKind.Program)
            {
                code = scriptCode;
            }
            else
            {
                throw new NotImplementedException($"Code parsing is not implemented yet for script kind: {script.Config.Kind}");
            }

            code = string.Format(program, code, usings);
            return code
                .Replace("Console.WriteLine", "UserScript.ConsoleWriteLine")
                .Replace("Console.Write", "UserScript.ConsoleWrite");
        }
    }
}
