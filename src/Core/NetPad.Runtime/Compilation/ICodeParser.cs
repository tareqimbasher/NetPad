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
    /// <param name="script">The target script.</param>
    /// <param name="code">If not null, will be used instead of <see cref="Script.Code"/>.</param>
    /// <param name="options">Parsing options.</param>
    CodeParsingResult Parse(
        Script script,
        string? code = null,
        CodeParsingOptions? options = null);
}
