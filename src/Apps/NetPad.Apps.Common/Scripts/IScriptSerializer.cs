using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Apps.Scripts;

/// <summary>
/// Serializes and deserializes scripts for a specific file format.
/// </summary>
public interface IScriptSerializer
{
    /// <summary>The file format this serializer handles.</summary>
    ScriptFileFormat Format { get; }

    /// <summary>
    /// The file extension (with leading dot) associated with this format, e.g. <c>.netpad</c>.
    /// </summary>
    string FileExtension { get; }

    /// <summary>Serializes a script to its on-disk representation.</summary>
    string Serialize(Script script);

    /// <summary>Deserializes a script from its on-disk representation.</summary>
    Task<Script> DeserializeAsync(
        string name,
        string data,
        IDataConnectionRepository dataConnectionRepository,
        IDotNetInfo dotNetInfo);

    /// <summary>
    /// Reads just enough of the file at <paramref name="path"/> to extract the script ID and kind
    /// without fully deserializing the script. Returns <see langword="false"/> if the file cannot
    /// be parsed by this serializer.
    /// </summary>
    bool TryReadSummary(string path, out Guid? id, out ScriptKind? kind);
}
