using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Data.Metadata;

public class DataConnectionSourceCode
{
    public SourceCodeCollection ApplicationCode { get; init; } = [];

    public bool IsEmpty() => ApplicationCode.Count == 0;
}
