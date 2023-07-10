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

        return new CodeParsingResult(
            new SourceCode(userProgram, namespaces),
            bootstrapperProgramSourceCode,
            options?.AdditionalCode,
            new ParsedCodeInformation(BootstrapperClassName, BootstrapperSetIOMethodName));
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

            userCode = $@"
await using var command = DataContext.Database.GetDbConnection().CreateCommand();

command.CommandText = @""{scriptCode}"";
await DataContext.Database.OpenConnectionAsync();

try
{{
    await using var reader = await command.ExecuteReaderAsync();

    do
    {{
        var dataTable = new DataTable();
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
        }
        else
        {
            userCode = scriptCode;
        }

        return userCode;
    }

    public string GetBootstrapperProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ScriptRuntimeServices).Assembly, $"{nameof(ScriptRuntimeServices)}.cs");
    }
}
