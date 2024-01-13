using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.Data;
using NetPad.DotNet;
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
                ContainsProductionData = dbConnection.ContainsProductionData,
                ConnectionStringAugment = dbConnection.ConnectionStringAugment
            };
        }
        else if (script.DataConnection != null)
        {
            serializedDataConnection = new SerializedDataConnection
            {
                Id = script.DataConnection.Id,
                Name = script.DataConnection.Name,
                Type = script.DataConnection.Type
            };
        }

        var scriptData = new ScriptData(ScriptConfigData.From(script.Config), serializedDataConnection);

        return $"{script.Id}\n" +
               $"{JsonSerializer.Serialize(scriptData)}\n" +
               "#Code\n" +
               $"{script.Code}";
    }

    public static async Task<Script> DeserializeAsync(string name, string data, IDataConnectionRepository dataConnectionRepository, IDotNetInfo dotNetInfo)
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

        var scriptData = DeserializeScriptData(scriptDataStr) ?? throw new InvalidScriptFormatException(name, "Could not deserialize config data");

        // Parse code
        var code = string.Join("\n", lines.Skip(ixCodeMarker + 1));

        var script = new Script(id, name, scriptData.Config.ToScriptConfig(dotNetInfo), code);

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

    public static ScriptData? DeserializeScriptData(string json)
    {
        var scriptData = JsonSerializer.Deserialize<ScriptData>(json);

        if (scriptData == null || scriptData.Config == null!)
        {
            var config = JsonSerializer.Deserialize<ScriptConfigData>(json);

            if (config == null) return null;

            scriptData = new ScriptData(config, null);
        }

        return scriptData;
    }


    public class ScriptData
    {
        public ScriptData(ScriptConfigData config, SerializedDataConnection? dataConnection)
        {
            Config = config;
            DataConnection = dataConnection;
        }

        public ScriptConfigData Config { get; }
        public SerializedDataConnection? DataConnection { get; }
    }

    public class ScriptConfigData
    {
        public ScriptKind? Kind { get; set; }
        public DotNetFrameworkVersion? TargetFrameworkVersion { get; set; }
        public OptimizationLevel OptimizationLevel { get; set; }
        public bool UseAspNet { get; set; }
        public List<string>? Namespaces { get; set; }
        public List<Reference>? References { get; set; }

        public static ScriptConfigData From(ScriptConfig config)
        {
            return new ScriptConfigData()
            {
                Kind = config.Kind,
                TargetFrameworkVersion = config.TargetFrameworkVersion,
                OptimizationLevel = config.OptimizationLevel,
                UseAspNet = config.UseAspNet,
                Namespaces = config.Namespaces,
                References = config.References
            };
        }

        public ScriptConfig ToScriptConfig(IDotNetInfo dotNetInfo)
        {
            return new ScriptConfig(
                Kind ?? ScriptKind.Program,
                TargetFrameworkVersion ?? dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.FrameworkVersion() ?? GlobalConsts.AppDotNetFrameworkVersion,
                Namespaces,
                References,
                OptimizationLevel,
                UseAspNet
            );
        }
    }

    public class SerializedDataConnection
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DataConnectionType Type { get; set; }
    }

    public class SerializedDatabaseConnection : SerializedDataConnection
    {
        public string? Host { get; set; }
        public string? Port { get; set; }
        public string? DatabaseName { get; set; }
        public string? UserId { get; set; }
        public bool ContainsProductionData { get; set; }
        public string? ConnectionStringAugment { get; set; }
    }
}
