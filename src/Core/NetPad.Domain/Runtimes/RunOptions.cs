using System.Collections.Generic;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Runtimes;

public class RunOptions
{
    public RunOptions()
    {
        AdditionalCode = new SourceCodeCollection();
        AdditionalReferences = new List<Reference>();
    }

    public RunOptions(string? specificCodeToRun) : this()
    {
        SpecificCodeToRun = specificCodeToRun;
    }

    public string? SpecificCodeToRun { get; set; }
    public SourceCodeCollection AdditionalCode { get; }
    public List<Reference> AdditionalReferences { get; }
}
