using NetPad.Scripts;

namespace NetPad.Compilation
{
    public interface ICodeParser
    {
        string GetCode(Script script);
    }
}
