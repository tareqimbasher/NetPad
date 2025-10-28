using NetPad.Compilation;
using NetPad.DotNet.CodeAnalysis;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.InMemory;

public class InMemoryRunnerCSharpCodeParser : ICodeParser
{
    public const string BootstrapperClassName = "ScriptRuntimeServices";
    public const string BootstrapperSetIOMethodName = "SetIO";

    private static readonly string[] _usingsNeededByBaseProgram =
    [
        "System",
        "NetPad.IO",
        "NetPad.Runtimes"
    ];

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

        var bootstrapperProgramTemplate = GetBootstrapperProgramTemplate();
        var bootstrapperProgram = string.Format(
            bootstrapperProgramTemplate,
            BootstrapperClassName,
            BootstrapperSetIOMethodName);

        var bootstrapperProgramSourceCode = new SourceCode(bootstrapperProgram, _usingsNeededByBaseProgram);

        return new CodeParsingResult(
            userProgram,
            bootstrapperProgramSourceCode,
            options?.AdditionalCode);
    }

    public SourceCode GetUserProgram(Script script, string userCode, CodeParsingOptions? options)
    {
        SourceCode userProgram;

        if (script.Config.Kind == ScriptKind.Expression)
        {
            throw new NotImplementedException("Expression code parsing is not implemented yet.");
        }

        if (script.Config.Kind == ScriptKind.SQL)
        {
            userCode = userCode.Replace("\"", "\"\"");

            userCode = $@"
await using var command = DataContext.Database.GetDbConnection().CreateCommand();

command.CommandText = @""{userCode}"";
await DataContext.Database.OpenConnectionAsync();

try
{{
    await using var reader = await command.ExecuteReaderAsync();

    do
    {{
        var dataTable = new System.Data.DataTable();
        dataTable.Load(reader);

        if (dataTable.Rows.Count > 0)
            dataTable.Dump();
        else
            ""No rows returned"".Dump();
    }} while (!reader.IsClosed);

    return 0;
}}
catch (System.Exception ex)
{{
    ex.Message.Dump();
    return 1;
}}
";

            userProgram = new SourceCode(userCode);
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

    public string GetBootstrapperProgramTemplate()
    {
        return $@"class {{0}}
{{{{
    internal static IOutputWriter<object> Output {{{{ get; set; }}}}

    private static void {{1}}(IOutputWriter<object> output)
    {{{{
        Output = output;
    }}}}

    internal static void OutputWrite(object? o = null, string? title = null)
    {{{{
        Output.WriteAsync(new RawScriptOutput(o), title);
    }}}}

    internal static void OutputWriteLine(object? o = null, string? title = null)
    {{{{
        Output.WriteAsync(new RawScriptOutput(o), title);
    }}}}

    internal static void SqlWrite(object? o, string? title = null)
    {{{{
        {{0}}.Output.WriteAsync(new SqlScriptOutput(o?.ToString()), title);
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
}}}}
";
    }
}
