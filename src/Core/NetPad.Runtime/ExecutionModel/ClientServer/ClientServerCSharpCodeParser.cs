using NetPad.Compilation;
using NetPad.DotNet.CodeAnalysis;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.ClientServer;

public class ClientServerCSharpCodeParser : ICodeParser
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

    public static string GetEmbeddedBootstrapperProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ClientServerCSharpCodeParser).Assembly, "EmbeddedCode.Program.cs");
    }

    public static string GetEmbeddedSqlProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ClientServerCSharpCodeParser).Assembly, "EmbeddedCode.SqlAccessCode.cs");
    }
}
