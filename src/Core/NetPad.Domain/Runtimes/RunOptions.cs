using System.Collections.Generic;
using NetPad.DotNet;
using NetPad.IO;

namespace NetPad.Runtimes;

/// <summary>
/// An asset used when a script runs.
/// <param name="CopyFrom">The absolute path of the file to copy.</param>
/// <param name="CopyTo">The relative path (relative to the working directory of the running script)
/// of the location to copy to. This must be a relative file path. </param>
/// </summary>
public record RunAsset(FilePath CopyFrom, RelativePath CopyTo);

/// <summary>
/// Options that define how a script will be ran.
/// </summary>
public class RunOptions
{
    public RunOptions()
    {
        AdditionalCode = new SourceCodeCollection();
        AdditionalReferences = new List<Reference>();
        Assets = new HashSet<RunAsset>();
    }

    public RunOptions(string? specificCodeToRun) : this()
    {
        SpecificCodeToRun = specificCodeToRun;
    }

    /// <summary>
    /// If not null, this code will run instead of script code.
    /// </summary>
    public string? SpecificCodeToRun { get; }

    /// <summary>
    /// Additional code to include while running the script.
    /// </summary>
    public SourceCodeCollection AdditionalCode { get; }

    /// <summary>
    /// Additional references to include alongside references configured in script.
    /// </summary>
    public List<Reference> AdditionalReferences { get; }

    /// <summary>
    /// File assets that are needed to run the script. These files are copied from their location
    /// to the defined relative path (relative to the working directory of the running script).
    /// </summary>
    public HashSet<RunAsset> Assets { get; }
}
