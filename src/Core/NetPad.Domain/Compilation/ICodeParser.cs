using NetPad.Scripts;

namespace NetPad.Compilation
{
    public interface ICodeParser
    {
        CodeParsingResult Parse(Script script, params string[] additionalNamespaces);
    }
}
