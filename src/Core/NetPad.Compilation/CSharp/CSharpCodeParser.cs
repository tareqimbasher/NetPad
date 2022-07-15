using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Compilation.CSharp
{
    public class CSharpCodeParser : ICodeParser
    {
        public const string UserCodeMarker = "// USER CODE STARTS BELOW THIS LINE";
        public const string BootstrapperClassName = "ScriptProgram_Bootstrap";
        public const string BootstrapperSetIOMethodName = "SetIO";

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
            var userProgram = string.Format(userProgramTemplate, userCode);

            var fullProgramTemplate = GetFullProgramTemplate();
            var fullProgram = string.Format(
                fullProgramTemplate,
                usings,
                userProgram,
                BootstrapperClassName,
                BootstrapperSetIOMethodName);

            int userCodeStartLine = 0;
            using var reader = new StringReader(fullProgram);
            string? line = "";
            int lineNumber = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (line.Contains(UserCodeMarker))
                {
                    userCodeStartLine = lineNumber;
                    break;
                }
            }

            return new CodeParsingResult(fullProgram, userProgram,
                new ParsedCodeInformation(userCodeStartLine, namespaces.ToHashSet(), BootstrapperClassName, BootstrapperSetIOMethodName));
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
            else
            {
                userCode = scriptCode;
            }

            return userCode;
        }

        public string GetFullProgramTemplate()
        {
            return @"{0}



{1}



class {2}
{{
    private static IOutputWriter OutputWriter {{ get; set; }}

    // Entry point used when running script in external process
    static async Task Main(string[] args)
    {{
        {3}(new ActionOutputWriter((o, t) => Console.WriteLine(o?.ToString())));
    }}

    private static void {3}(IOutputWriter outputWriter)
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
        {2}.OutputWriteLine(o, title);
        return o;
    }}
}}
";
        }

        public string GetUserProgramTemplate()
        {
            return $"{UserCodeMarker}\n{{0}}";
        }
    }
}
