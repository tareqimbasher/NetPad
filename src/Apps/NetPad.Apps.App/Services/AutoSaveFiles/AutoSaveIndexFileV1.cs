using System.Collections.Generic;
using NetPad.Common;

namespace NetPad.Services.AutoSaveFiles;

public class AutoSaveIndexFileV1 : IVersionedJson
{
    public int Version => 1;
    public Dictionary<Guid, AutoSaveIndexEntry> Entries { get; set; } = new();
}

public record AutoSaveIndexEntry(string? Name, string? OriginalPath);
