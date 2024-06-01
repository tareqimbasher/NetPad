using NetPad.Scripts;

namespace NetPad.Compilation;

public interface ICodeParser
{
    /// <summary>
    /// Parses script code into a full, ready to compile, program.
    /// </summary>
    /// <param name="scriptCode">The code to parse.</param>
    /// <param name="scriptKind">The kind of code.</param>
    /// <param name="usings">Usings to be included in parsed code.</param>
    /// <param name="options">Parsing options.</param>
    CodeParsingResult Parse(
        string scriptCode,
        ScriptKind scriptKind,
        IEnumerable<string>? usings = null,
        CodeParsingOptions? options = null);
}
