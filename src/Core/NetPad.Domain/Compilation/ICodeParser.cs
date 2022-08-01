using NetPad.Scripts;

namespace NetPad.Compilation
{
    public interface ICodeParser
    {
        /// <summary>
        /// Parses script code into a full, ready to compile, program.
        /// </summary>
        /// <param name="script">The target script.</param>
        /// <param name="code">If specified, will only include the given code in the parsed program. Otherwise, all script code will be parsed.</param>
        /// <param name="additionalNamespaces">Additional namespaces to include in the program.</param>
        CodeParsingResult Parse(Script script, string? code = null, params string[] additionalNamespaces);
    }
}
