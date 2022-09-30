using NetPad.DotNet;

namespace NetPad.Data;

public class DataConnectionSourceCode
{
    public DataConnectionSourceCode()
    {
        DataAccessCode = new SourceCodeCollection();
        ApplicationCode = new SourceCodeCollection();
    }

    public SourceCodeCollection DataAccessCode { get; init; }
    public SourceCodeCollection ApplicationCode { get; init; }
}
