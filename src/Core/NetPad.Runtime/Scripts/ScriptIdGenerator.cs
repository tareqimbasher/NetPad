using NetPad.Common;

namespace NetPad.Scripts;

/// <summary>
/// Generates IDs for scripts.
/// </summary>
/// <remarks>
/// For properly formatted NetPad script files (ie. files ending with <see cref="Script.STANDARD_EXTENSION"/>)
/// the ID for a new script is just a new Guid.
///
/// For files that aren't formatted NetPad script files (ex: a .cs file that we convert to a script), we
/// generate an ID from that file's full path.
/// </remarks>
public static class ScriptIdGenerator
{
    public static Guid NewId() => Guid.NewGuid();
    public static Guid IdFromFilePath(string filePath) => Uuid5.Create(filePath);
}
