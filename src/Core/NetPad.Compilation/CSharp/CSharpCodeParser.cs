using System;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Scripts;

namespace NetPad.Compilation.CSharp
{
    public class CSharpCodeParser : ICodeParser
    {
        public const string BootstrapperClassName = "ScriptProgram_Bootstrap";
        public const string BootstrapperSetIOMethodName = "SetIO";

        private static readonly string[] _usingsNeededByBaseProgram =
        {
            "System",
            "NetPad.IO"
        };

        public CodeParsingResult Parse(Script script, CodeParsingOptions? options = null)
        {
            var userProgram = GetUserProgram(options?.IncludedCode ?? script.Code, script.Config.Kind);

            var bootstrapperProgramTemplate = GetBootstrapperProgramTemplate();
            var bootstrapperProgram = string.Format(
                bootstrapperProgramTemplate,
                BootstrapperClassName,
                BootstrapperSetIOMethodName);

            var bootstrapperProgramSourceCode = new SourceCode(bootstrapperProgram, _usingsNeededByBaseProgram);

            return new CodeParsingResult(
                new SourceCode(userProgram, script.Config.Namespaces),
                bootstrapperProgramSourceCode,
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
            return $@"class {{0}}
{{{{
    internal static {nameof(IScriptOutput)} Output {{{{ get; set; }}}}

    // Entry point used when running script in external process
    static async System.Threading.Tasks.Task Main(string[] args)
    {{{{
        // {{1}}(new ActionOutputWriter((o, t) => Console.WriteLine(o?.ToString())));
    }}}}

    private static void {{1}}({nameof(IScriptOutput)} output)
    {{{{
        Output = output;
    }}}}

    internal static void OutputWrite(object? o = null, string? title = null)
    {{{{
        Output.{nameof(IScriptOutput.PrimaryChannel)}.WriteAsync(o, title);
    }}}}

    internal static void OutputWriteLine(object? o = null, string? title = null)
    {{{{
        Output.{nameof(IScriptOutput.PrimaryChannel)}.WriteAsync(o, title);
    }}}}
}}}}

static class Exts
{{{{
    /// <summary>
    /// Dumps this object to the results view.
    /// </summary>
    /// <param name=""o"">The object being dumped.</param>
    /// <param name=""title"">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(""o"")]
    public static T? Dump<T>(this T? o, string? title = null)
    {{{{
        {{0}}.OutputWriteLine(o, title);
        return o;
    }}}}

    internal static void DumpToSqlOutput<T>(this T? o, string? title = null)
    {{{{
        {{0}}.Output.{nameof(IScriptOutput.SqlChannel)}?.WriteAsync(o, title);
    }}}}
}}}}
";
        }
    }
}
