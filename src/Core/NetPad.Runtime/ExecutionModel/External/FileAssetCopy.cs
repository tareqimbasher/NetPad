using NetPad.IO;

namespace NetPad.ExecutionModel.External;

/// <summary>
/// A file that should be copied to script start directory before a script runs.
/// <param name="CopyFrom">The absolute path of the file to copy.</param>
/// <param name="CopyTo">The relative path (relative to the directory the script will run from)
/// to copy to. This must be a relative file path. </param>
/// </summary>
public record FileAssetCopy(FilePath CopyFrom, RelativePath CopyTo);
