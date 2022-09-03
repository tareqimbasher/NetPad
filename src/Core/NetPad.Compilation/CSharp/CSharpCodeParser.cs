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

        public static readonly string[] NamespacesNeededByBaseProgram =
        {
            "System",
            "System.Threading.Tasks",
            "NetPad.IO"
        };

        public CodeParsingResult Parse(Script script, CodeParsingOptions? options = null)
        {
            var namespaces = GetNamespaces(script, options?.AdditionalCode.GetAllNamespaces());

            var userCode = GetUserCode(options?.IncludedCode ?? script.Code, script.Config.Kind);
            var userProgramTemplate = GetUserProgramTemplate();
            var userProgram = string.Format(userProgramTemplate, userCode);

            var bootstrapperProgramTemplate = GetBootstrapperProgramTemplate();
            var bootstrapperProgram = string.Format(
                bootstrapperProgramTemplate,
                BootstrapperClassName,
                BootstrapperSetIOMethodName);

            string? additionalCodeProgram = options?.AdditionalCode.GetAllCode();

            return new CodeParsingResult(
                namespaces,
                userProgram,
                bootstrapperProgram,
                additionalCodeProgram,
                new ParsedCodeInformation(BootstrapperClassName, BootstrapperSetIOMethodName));
        }

        public HashSet<string> GetNamespaces(Script script, IEnumerable<string>? additionalNamespaces = null)
        {
            additionalNamespaces ??= Array.Empty<string>();

            return NamespacesNeededByBaseProgram
                .Union(script.Config.Namespaces.Where(ns => !string.IsNullOrWhiteSpace(ns)))
                .Union(additionalNamespaces.Where(ns => !string.IsNullOrWhiteSpace(ns)))
                .Select(ns => ns.Trim())
                .ToHashSet();
        }

        public string GetUserCode(string code, ScriptKind kind)
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

        public string GetUserProgramTemplate()
        {
            return $"{{0}}";
        }
    }
}
