using NetPad.DotNet;

namespace NetPad.Data;

public class DataConnectionSourceCode
{
    public SourceCodeCollection DataAccessCode { get; init; } = [];
    public SourceCodeCollection ApplicationCode { get; init; } = [];

    public bool IsEmpty() => !DataAccessCode.Any() && !ApplicationCode.Any();
}
