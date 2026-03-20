using System.Text.Json.Nodes;
using NetPad.Common;

namespace NetPad.Apps.Data.DataConnectionFiles;

public class DataConnectionFileV0ToV1MigrationStep : IJsonMigrationStep
{
    public int FromVersion => 0;
    public int ToVersion => 1;

    public void Apply(JsonObject doc)
    {
        var keys = doc.Select(kvp => kvp.Key).ToList();

        var connections = new JsonArray();

        foreach (var key in keys)
        {
            if (!doc.TryGetPropertyValue(key, out var node) || node is null)
                continue;

            // Detach node from doc
            doc.Remove(key);

            if (node is not JsonObject connection)
                continue;

            connection["id"] ??= key; // Ensure ID is set
            connections.Add(connection);
        }

        doc.Clear();
        doc["version"] = ToVersion;
        doc["connections"] = connections;
        doc["databaseServers"] = new JsonArray();
    }
}
