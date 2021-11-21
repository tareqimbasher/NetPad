using System;
using NetPad.Scripts;

namespace NetPad.Runtimes
{
    public class CodeParser
    {
        public static string GetScriptCode(Script script)
        {
            string scriptCode = script.Code;
            string code;

            const string program = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetPad.Runtimes;

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

            code = string.Format(program, code);
            return code
                .Replace("Console.WriteLine", "UserScript.ConsoleWriteLine")
                .Replace("Console.Write", "UserScript.ConsoleWrite");
        }
    }
}
