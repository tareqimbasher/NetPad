using NetPad.DotNet;

namespace NetPad.Compilation;

public class CodeParsingOptions
{
    public CodeParsingOptions()
    {
        AdditionalCode = new SourceCodeCollection();
    }

    /// <summary>
    /// If specified, will only include the given user code in the parsed program. Otherwise, all user code in script will be parsed.
    /// </summary>
    public string? IncludedCode { get; set; }

    /// <summary>
    /// Additional code, that is not user code, to include in the program.
    /// </summary>
    public SourceCodeCollection AdditionalCode { get; set; }
}
