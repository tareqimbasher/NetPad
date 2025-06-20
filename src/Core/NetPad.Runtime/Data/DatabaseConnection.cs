using NetPad.Data.Security;

namespace NetPad.Data;

/// <summary>
/// A connection to a database.
/// </summary>
public abstract class DatabaseConnection(Guid id, string name, DataConnectionType type) : DataConnection(id, name, type)
{
    public string? Host { get; set; }
    public string? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public bool ContainsProductionData { get; set; }

    /// <summary>
    /// A partial connection string that is used to override values in the final connection string.
    /// For example, if this value is Timeout=300:
    ///   - When connection string is "Server=file.db;Password=123;Timeout=100" the resulting final
    ///     connection string will be:
    ///       "Server=file.db;Password=123;Timeout=300"
    ///   - When connection string is "Server=file.db;Password=123;" the resulting final
    ///     connection string will be:
    ///       "Server=file.db;Password=123;Timeout=300"
    /// </summary>
    public string? ConnectionStringAugment { get; set; }

    public abstract string GetConnectionString(IDataConnectionPasswordProtector passwordProtector);
}
