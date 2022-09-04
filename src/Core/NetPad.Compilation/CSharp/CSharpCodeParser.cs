using System;
using System.Collections.Generic;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Compilation.CSharp
{
    public class CSharpCodeParser : ICodeParser
    {
        public const string BootstrapperClassName = "ScriptProgram_Bootstrap";
        public const string BootstrapperSetIOMethodName = "SetIO";

        public CodeParsingResult Parse(Script script, CodeParsingOptions? options = null)
        {
            var userProgram = GetUserProgram(options?.IncludedCode ?? script.Code, script.Config.Kind);

            var bootstrapperProgramTemplate = GetBootstrapperProgramTemplate();
            var bootstrapperProgram = string.Format(
                bootstrapperProgramTemplate,
                BootstrapperClassName,
                BootstrapperSetIOMethodName);

            string? additionalCodeProgram = options?.AdditionalCode.GetAllCode();

            return new CodeParsingResult(
                new SourceCode(userProgram, script.Config.Namespaces),
                new SourceCode(bootstrapperProgram),
                options?.AdditionalCode,
                new ParsedCodeInformation(BootstrapperClassName, BootstrapperSetIOMethodName));
        }

        public string GetUserProgram(string code, ScriptKind kind)
        {
            string userCode;
            string scriptCode = code;

            if (kind == ScriptKind.Expression)
            {
                throw new NotImplementedException("Expression code parsing is not implemented yet.");
            }
            else
            {
                userCode = scriptCode;
            }

            return userCode;
        }

        public string GetBootstrapperProgramTemplate()
        {
            return @"class {0}
{{
    private static IOutputWriter OutputWriter {{ get; set; }}

    // Entry point used when running script in external process
    static async Task Main(string[] args)
    {{
        {1}(new ActionOutputWriter((o, t) => Console.WriteLine(o?.ToString())));
    }}

    private static void {1}(IOutputWriter outputWriter)
    {{
        OutputWriter = outputWriter;
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

static class Exts
{{
    /// <summary>
    /// Dumps this object to the results view.
    /// </summary>
    /// <param name=""o"">The object being dumped.</param>
    /// <param name=""title"">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    public static T? Dump<T>(this T? o, string? title = null)
    {{
        {0}.OutputWriteLine(o, title);
        return o;
    }}
}}
";
        }
    }
}
