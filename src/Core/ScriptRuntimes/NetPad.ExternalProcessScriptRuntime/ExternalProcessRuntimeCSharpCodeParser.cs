using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Runtimes;

public class ExternalProcessRuntimeCSharpCodeParser : ICodeParser
{
    public const string BootstrapperClassName = nameof(ScriptRuntimeServices);
    public const string BootstrapperSetIOMethodName = nameof(ScriptRuntimeServices.SetIO);

    public CodeParsingResult Parse(
        string code,
        ScriptKind scriptKind,
        IEnumerable<string>? namespaces = null,
        CodeParsingOptions? options = null)
    {
        var userProgram = GetUserProgram(code, scriptKind);

        var bootstrapperProgram = GetBootstrapperProgram();

        var bootstrapperProgramSourceCode = SourceCode.Parse(bootstrapperProgram);

        var additionalCode = options != null ? new SourceCodeCollection(options.AdditionalCode) : new SourceCodeCollection();

        if (options?.IncludeAspNetUsings == true)
        {
            namespaces = AddAspNetNamespaces(namespaces);
        }

        return new CodeParsingResult(
            new SourceCode(userProgram, namespaces),
            bootstrapperProgramSourceCode,
            additionalCode,
            new ParsedCodeInformation(BootstrapperClassName, BootstrapperSetIOMethodName));
    }

    private IEnumerable<string>? AddAspNetNamespaces(IEnumerable<string>? namespaces)
    {
        var list = namespaces != null ? namespaces.ToList() : new List<string>();

        list.AddRange(new[]
        {
            "System.Net.Http.Json",
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Routing",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging",
        });

        return list;
    }

    public string GetUserProgram(string scriptCode, ScriptKind kind)
    {
        string userCode;

        if (kind == ScriptKind.Expression)
        {
            throw new NotImplementedException("Expression code parsing is not implemented yet.");
        }

        if (kind == ScriptKind.SQL)
        {
            scriptCode = scriptCode.Replace("\"", "\"\"");

            userCode = AssemblyUtil.ReadEmbeddedResource(typeof(ExternalProcessRuntimeCSharpCodeParser).Assembly, "EmbeddedCode.SqlAccessCode.cs")
                .Replace("SQL_CODE", scriptCode);
        }
        else
        {
            userCode = scriptCode;
        }

        return userCode;
    }

    private static string GetBootstrapperProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ExternalProcessRuntimeCSharpCodeParser).Assembly, "EmbeddedCode.Program.cs");
    }
}
