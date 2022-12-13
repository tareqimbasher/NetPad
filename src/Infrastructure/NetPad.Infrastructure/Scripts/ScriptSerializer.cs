using System;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Data;
using NetPad.Exceptions;

namespace NetPad.Scripts;

public static class ScriptSerializer
{
    public static string Serialize(Script script)
    {
        SerializedDataConnection? serializedDataConnection = null;

        if (script.DataConnection is DatabaseConnection dbConnection)
        {
            serializedDataConnection = new SerializedDatabaseConnection
            {
                Id = dbConnection.Id,
                Name = dbConnection.Name,
                Type = dbConnection.Type,
                Host = dbConnection.Host,
                Port = dbConnection.Port,
                DatabaseName = dbConnection.DatabaseName,
                UserId = dbConnection.UserId,
                ContainsProductionData = dbConnection.ContainsProductionData
            };
        }
        else if (script.DataConnection != null)
        {
            serializedDataConnection = new SerializedDataConnection()
            {
                Id = script.DataConnection.Id,
                Name = script.DataConnection.Name,
                Type = script.DataConnection.Type,
            };
        }

        var scriptData = new ScriptData(script.Config, serializedDataConnection);

        return $"{script.Id}\n" +
               $"{JsonSerializer.Serialize(scriptData)}\n" +
               $"#Code\n" +
               $"{script.Code}";
    }

    public static async Task<Script> DeserializeAsync(string name, string data, IDataConnectionRepository dataConnectionRepository)
    {
        var lines = data.Split("\n").ToList();

        // Parse ID
        if (!Guid.TryParse(lines[0], out var id) || id == default)
            throw new InvalidScriptFormatException(name, "Invalid or non-existent ID.");

        int ixCodeMarker = lines.FindIndex(l => l.Trim() == "#Code");
        if (ixCodeMarker < 0)
            throw new InvalidScriptFormatException(name, "The script is missing #Code identifier.");

        // Parse script data (skip first line (ID) and take up to code marker)
        var scriptDataStr = string.Join("\n", lines.Skip(1).Take(lines.Count - (lines.Count - ixCodeMarker + 1))).Trim();
        if (string.IsNullOrWhiteSpace(scriptDataStr))
            throw new InvalidScriptFormatException(name, "The script is missing its config data.");

        var scriptData = JsonSerializer.Deserialize<ScriptData>(scriptDataStr);
        if (scriptData == null || scriptData.Config == null!)
        {
            var config = JsonSerializer.Deserialize<ScriptConfig>(scriptDataStr)
                         ?? throw new InvalidScriptFormatException(name, "Could not deserialize config data");
            scriptData = new ScriptData(config, null);
        }

        // Parse code
        var code = string.Join("\n", lines.Skip(ixCodeMarker + 1));

        var script = new Script(id, name, scriptData.Config, code);

        if (scriptData.DataConnection != null)
        {
            var connection = await dataConnectionRepository.GetAsync(scriptData.DataConnection.Id);

            if (connection == null)
            {
                // TODO create a new connection
            }

            if (connection != null)
                script.SetDataConnection(connection);
        }

        return script;
    }


    private class ScriptData
    {
        public ScriptData(ScriptConfig config, SerializedDataConnection? dataConnection)
        {
            Config = config;
            DataConnection = dataConnection;
        }

        public ScriptConfig Config { get; }
        public SerializedDataConnection? DataConnection { get; }
    }

    private class SerializedDataConnection
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DataConnectionType Type { get; set; }
    }

    private class SerializedDatabaseConnection : SerializedDataConnection
    {
        public string? Host { get; set; }
        public string? Port { get; set; }
        public string? DatabaseName { get; set; }
        public string? UserId { get; set; }
        public bool ContainsProductionData { get; set; }
    }
}
