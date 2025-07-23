using System.Runtime.Serialization;
using Newtonsoft.Json;
using NJsonSchema.Converters;

namespace NetPad.Plugins.OmniSharp.Features.Common.FileOperation;

// Only used for NSwag
[JsonConverter(typeof(JsonInheritanceConverter<FileOperationResponse>), "discriminator")]
[System.Text.Json.Serialization.JsonConverter(typeof(NetPad.Common.JsonInheritanceConverter<FileOperationResponse>))]
[KnownType(typeof(ModifiedFileResponse))]
[KnownType(typeof(OpenFileResponse))]
[KnownType(typeof(RenamedFileResponse))]
public abstract class FileOperationResponse(string fileName, OmniSharpFileModificationType type)
{
    public string FileName { get; } = fileName;

    public OmniSharpFileModificationType ModificationType { get; } = type;
}

public class ModifiedFileResponse(string fileName) : FileOperationResponse(fileName, OmniSharpFileModificationType.Modified)
{
    public string Buffer { get; set; } = null!;
    public IEnumerable<OmniSharpLinePositionSpanTextChange> Changes { get; set; } = null!;
}

public class OpenFileResponse(string fileName) : FileOperationResponse(fileName, OmniSharpFileModificationType.Opened);

public class RenamedFileResponse(string fileName, string newFileName) : FileOperationResponse(fileName, OmniSharpFileModificationType.Renamed)
{
    public string NewFileName { get; } = newFileName;
}
