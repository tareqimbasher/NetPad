using NetPad.Scripts;

namespace NetPad.Compilation;

public interface ICodeParser
{
    /// <summary>
    /// Parses script code into a full, ready to compile, program.
    /// </summary>
    /// <param name="script">The target script.</param>
    /// <param name="options">Parsing options.</param>
    CodeParsingResult Parse(Script script, CodeParsingOptions? options = null);
}
