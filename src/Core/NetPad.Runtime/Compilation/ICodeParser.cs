using NetPad.Scripts;

namespace NetPad.Compilation;

/// <summary>
/// Parses user script code into a program that is ready to compile.
/// </summary>
public interface ICodeParser
{
    /// <summary>
    /// Parses script code into a program that is ready to compile.
    /// </summary>
    /// <param name="scriptCode">The code to parse.</param>
    /// <param name="scriptKind">The kind of code.</param>
    /// <param name="usings">Additional usings to include in parsed program.</param>
    /// <param name="options">Parsing options.</param>
    CodeParsingResult Parse(
        string scriptCode,
        ScriptKind scriptKind,
        IEnumerable<string>? usings = null,
        CodeParsingOptions? options = null);
}
