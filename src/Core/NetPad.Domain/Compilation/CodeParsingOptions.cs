using NetPad.DotNet;

namespace NetPad.Compilation;

public class CodeParsingOptions
{
    public CodeParsingOptions()
    {
        AdditionalCode = new SourceCodeCollection();
    }

    /// <summary>
    /// Additional code, that is not user code, to include in the program.
    /// </summary>
    public SourceCodeCollection AdditionalCode { get; init; }
}
