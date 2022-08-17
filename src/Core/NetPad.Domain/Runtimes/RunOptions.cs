using NetPad.Compilation;

namespace NetPad.Runtimes;

public class RunOptions
{
    public RunOptions()
    {
        AdditionalCode = new SourceCodeCollection();
    }

    public RunOptions(string? specificCodeToRun) : this()
    {
        SpecificCodeToRun = specificCodeToRun;
    }

    public string? SpecificCodeToRun { get; set; }
    public SourceCodeCollection AdditionalCode { get; }
}
