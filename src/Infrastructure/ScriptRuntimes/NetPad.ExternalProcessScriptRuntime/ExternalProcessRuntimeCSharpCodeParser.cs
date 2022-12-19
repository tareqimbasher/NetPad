using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Runtimes
{
    public class ExternalProcessRuntimeCSharpCodeParser : ICodeParser
    {
        public const string BootstrapperClassName = nameof(ScriptUtils);
        public const string BootstrapperSetIOMethodName = nameof(ScriptUtils.SetIO);

        public CodeParsingResult Parse(Script script, CodeParsingOptions? options = null)
        {
            var userProgram = GetUserProgram(options?.IncludedCode ?? script.Code, script.Config.Kind);

            var bootstrapperProgram = GetBootstrapperProgram();

            var bootstrapperProgramSourceCode = SourceCode.Parse(bootstrapperProgram);

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

            userCode = scriptCode;

            return userCode;
        }

        public string GetBootstrapperProgram()
        {
            return AssemblyUtil.ReadEmbeddedResource(typeof(ScriptUtils).Assembly, "ScriptUtils.cs");
        }
    }
}
