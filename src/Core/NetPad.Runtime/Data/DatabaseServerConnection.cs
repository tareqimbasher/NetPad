using System.Runtime.Serialization;
using NetPad.Data.Security;
using Newtonsoft.Json;
using NJsonSchema.Converters;

namespace NetPad.Data;

/// <summary>
/// A connection to a database server.
/// </summary>
[JsonConverter(typeof(JsonInheritanceConverter<DatabaseServerConnection>), "discriminator")]
[System.Text.Json.Serialization.JsonConverter(typeof(Common.JsonInheritanceConverter<DatabaseServerConnection>))]
[KnownType("GetKnownTypes")]
public abstract class DatabaseServerConnection(Guid id, string name, DataConnectionType type)
    : DataConnection(id, name, type), IDatabaseConnection
{
    public string? Host { get; set; }
    public string? Port { get; set; }
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public bool ContainsProductionData { get; set; }

    /// <summary>
    /// A partial connection string that is used to add or override values in the final connection string.
    /// For example, if this value is Timeout=300:
    ///   - When connection string is "Server=file.db;Password=123;Timeout=100" the resulting final
    ///     connection string will be:
    ///       "Server=file.db;Password=123;Timeout=300"
    ///   - When connection string is "Server=file.db;Password=123;" the resulting final
    ///     connection string will be:
    ///       "Server=file.db;Password=123;Timeout=300"
    /// </summary>
    public string? ConnectionStringAugment { get; set; }

    /// <summary>
    /// The databases hosted on this server that the user has selected to include.
    /// </summary>
    public HashSet<string> SelectedDatabaseNames { get; set; } = [];

    public abstract DatabaseConnection CreateDatabaseConnection(string databaseName);

    public abstract string GetConnectionString(IDataConnectionPasswordProtector passwordProtector);

    public abstract Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector);

    private static Type[] GetKnownTypes()
    {
        var serverConnectionType = typeof(DatabaseServerConnection);

        return AppDomain.CurrentDomain.GetAssemblies()
        .Where(x => x.GetName().FullName.Contains("NetPad"))
        .SelectMany(x => x.GetExportedTypes())
        .Where(t => t is { IsClass: true, IsAbstract: false } && serverConnectionType.IsAssignableFrom(t))
        .ToArray();
    }

    private static class ServerConnectionKnownTypes
    {
        public static readonly Lazy<Type[]> KnownTypes = new(() =>
        {
            var serverConnectionType = typeof(DatabaseServerConnection);

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetName().FullName.Contains("NetPad"))
                .SelectMany(x => x.GetExportedTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && serverConnectionType.IsAssignableFrom(t))
                .ToArray();
        });
    }
}
