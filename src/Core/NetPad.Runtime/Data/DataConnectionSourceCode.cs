using NetPad.DotNet;

namespace NetPad.Data;

public class DataConnectionSourceCode
{
    public SourceCodeCollection ApplicationCode { get; init; } = [];

    public bool IsEmpty() => ApplicationCode.Count == 0;
}
