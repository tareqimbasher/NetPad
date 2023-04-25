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

    public string GetUserProgram(string code, ScriptKind kind)
    {
        string userCode;
        string scriptCode = code;

        if (kind == ScriptKind.Expression)
        {
            throw new NotImplementedException("Expression code parsing is not implemented yet.");
        }

        userCode = scriptCode;

        return userCode;
    }

    public string GetBootstrapperProgram()
    {
        return AssemblyUtil.ReadEmbeddedResource(typeof(ScriptRuntimeServices).Assembly, $"{nameof(ScriptRuntimeServices)}.cs");
    }
}
