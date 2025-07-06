using NetPad.Compilation;
using NetPad.DotNet.CodeAnalysis;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

public class ExternalRunnerCSharpCodeParser : ICodeParser
{
    private static readonly string[] _aspNetUsings =
    [
        "System.Net.Http.Json",
        "Microsoft.AspNetCore.Builder",
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Http",
        "Microsoft.AspNetCore.Routing",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Logging"
    ];

    public CodeParsingResult Parse(
        string scriptCode,
        ScriptKind scriptKind,
        IEnumerable<string>? usings = null,
        CodeParsingOptions? options = null)
    {
        var userProgramCode = new SourceCode(GetUserProgram(scriptCode, scriptKind));

        if (usings != null)
        {
            foreach (var u in usings)
            {
                userProgramCode.AddUsing(u);
            }
        }

        if (options?.IncludeAspNetUsings == true)
        {
            foreach (var u in _aspNetUsings)
            {
                userProgramCode.AddUsing(u);
            }
        }

        var bootstrapperProgramCode = SourceCode.Parse(GetEmbeddedBootstrapperProgram());

        var additionalCode = options?.AdditionalCode.ToSourceCodeCollection();

        return new CodeParsingResult(userProgramCode, bootstrapperProgramCode, additionalCode);
    }

    private static string GetUserProgram(string scriptCode, ScriptKind kind)
    {
        string userProgram;

        if (kind == ScriptKind.Expression)
        {
            throw new NotImplementedException("Expression code parsing is not implemented yet.");
        }

        if (kind == ScriptKind.SQL)
        {
            scriptCode = scriptCode.Replace("\"", "\"\"");

            userProgram = GetEmbeddedSqlProgram().Replace("SQL_CODE", scriptCode);
        }
        else
        {
            userProgram = scriptCode;
        }

        return userProgram;
    }

    internal static string GetEmbeddedBootstrapperProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ExternalRunnerCSharpCodeParser).Assembly, "EmbeddedCode.Program.cs");
    }

    internal static string GetEmbeddedSqlProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ExternalRunnerCSharpCodeParser).Assembly, "EmbeddedCode.SqlAccessCode.cs");
    }
}
