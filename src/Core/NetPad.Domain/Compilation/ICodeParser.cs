using System.Collections.Generic;
using NetPad.Scripts;

namespace NetPad.Compilation;

public interface ICodeParser
{
    /// <summary>
    /// Parses script code into a full, ready to compile, program.
    /// </summary>
    /// <param name="code">The code to parse.</param>
    /// <param name="scriptKind">The kind of code.</param>
    /// <param name="namespaces">Namespaces to be included in parsed code.</param>
    /// <param name="options">Parsing options.</param>
    CodeParsingResult Parse(
        string code,
        ScriptKind scriptKind,
        IEnumerable<string>? namespaces = null,
        CodeParsingOptions? options = null);
}
