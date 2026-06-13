using System.Text.Json.Nodes;
using NetPad.Common;

namespace NetPad.Services.AutoSaveFiles;

/// <summary>
/// Migrates an auto-save index file from the legacy v0 shape (a flat <c>Dictionary&lt;Guid, string&gt;</c>
/// mapping script id to script name) to v1 (a versioned object with <c>entries</c> keyed by script id,
/// each carrying a name and an optional original file path).
/// </summary>
public class AutoSaveIndexFileV0ToV1MigrationStep : IJsonMigrationStep
{
    public int FromVersion => 0;
    public int ToVersion => 1;

    public void Apply(JsonObject doc)
    {
        // v0 doc shape: {"<guid>": "<name>", ...}
        var entries = new JsonObject();

        foreach (var (key, node) in doc)
        {
            if (node is null)
            {
                continue;
            }

            // v0 value was the script name as a JSON string. Anything else: preserve the Guid with a
            // null name so the auto-save file stays recoverable (GetScriptAsync falls back to the
            // name generator).
            var name = node is JsonValue val && val.TryGetValue<string>(out var parsedName)
                ? parsedName
                : null;

            entries[key] = new JsonObject
            {
                ["name"] = name,
                ["originalPath"] = null
            };
        }

        doc.Clear();
        doc["version"] = ToVersion;
        doc["entries"] = entries;
    }
}
