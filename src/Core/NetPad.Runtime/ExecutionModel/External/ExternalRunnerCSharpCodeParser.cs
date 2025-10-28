using System.Text.RegularExpressions;
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

    public CodeParsingResult Parse(Script script, string? code = null, CodeParsingOptions? options = null)
    {
        var userCode = code ?? script.Code;
        var userProgram = GetUserProgram(script, userCode, options);
        var bootstrapperProgram = SourceCode.Parse(GetEmbeddedBootstrapperProgram(script));
        var additionalCode = options?.AdditionalCode.ToSourceCodeCollection();

        return new CodeParsingResult(userProgram, bootstrapperProgram, additionalCode);
    }

    private static SourceCode GetUserProgram(Script script, string userCode, CodeParsingOptions? options)
    {
        SourceCode userProgram;

        if (script.Config.Kind == ScriptKind.Expression)
        {
            throw new NotImplementedException("Expression code parsing is not implemented yet.");
        }

        if (script.Config.Kind == ScriptKind.SQL)
        {
            userCode = userCode.Replace("\"", "\"\"");
            userProgram = new SourceCode(GetEmbeddedSqlProgram().Replace("SQL_CODE", userCode));
        }
        else
        {
            userProgram = new SourceCode(userCode);
        }

        // Add usings
        var usings = script.Config.Namespaces.ToHashSet();

        if (options?.AdditionalUsings != null)
        {
            usings.AddRange(options.AdditionalUsings);
        }

        if (options?.IncludeAspNetUsings == true)
        {
            usings.AddRange(_aspNetUsings);
        }

        foreach (var u in usings)
        {
            userProgram.AddUsing(u);
        }

        return userProgram;
    }

    internal static string GetEmbeddedBootstrapperProgram(Script script)
    {
        var program = AssemblyUtil.ReadEmbeddedResource(typeof(ExternalRunnerCSharpCodeParser).Assembly,
            "ExecutionModel.External.EmbeddedCode.Program.cs");

        var replacements = new Dictionary<string, string>
        {
            { "USERSCRIPT_ID", script.Id.ToString() },
            { "USERSCRIPT_NAME", script.Name },
            { "\"USERSCRIPT_PATH\"", script.Path == null ? "null" : $"\"{script.Path}\"" }
        };
        var pattern = string.Join("|", replacements.Keys.Select(Regex.Escape));
        program = Regex.Replace(program, pattern, m => replacements[m.Value]);
        return program;
    }

    internal static string GetEmbeddedSqlProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ExternalRunnerCSharpCodeParser).Assembly,
            "ExecutionModel.External.EmbeddedCode.SqlAccessCode.cs");
    }
}
