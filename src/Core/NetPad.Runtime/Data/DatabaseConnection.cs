using System.Text.Json.Serialization;
using NetPad.Data.Security;

namespace NetPad.Data;

/// <summary>
/// A connection to a database.
/// </summary>
public abstract class DatabaseConnection(Guid id, string name, DataConnectionType type)
    : DataConnection(id, name, type), IDatabaseConnection
{
    private string? _host;
    private string? _port;
    private string? _userId;
    private string? _password;
    private bool _containsProductionData;
    private string? _connectionStringAugment;

    [JsonInclude] public Guid? ServerId { get; private set; }

    [JsonIgnore] public DatabaseServerConnection? Server { get; private set; }

    public string? Host
    {
        get => Server?.Host ?? _host;
        set
        {
            if (Server != null)
            {
                throw new InvalidOperationException(
                    $"Cannot set {nameof(Host)} on a server-attached database connection.");
            }

            _host = value;
        }
    }

    public string? Port
    {
        get => Server?.Port ?? _port;
        set
        {
            if (Server != null)
            {
                throw new InvalidOperationException(
                    $"Cannot set {nameof(Port)} on a server-attached database connection.");
            }

            _port = value;
        }
    }

    public string? DatabaseName { get; set; }

    public string? UserId
    {
        get => Server?.UserId ?? _userId;
        set
        {
            if (Server != null)
            {
                throw new InvalidOperationException(
                    $"Cannot set {nameof(UserId)} on a server-attached database connection.");
            }

            _userId = value;
        }
    }

    public string? Password
    {
        get => Server?.Password ?? _password;
        set
        {
            if (Server != null)
            {
                throw new InvalidOperationException(
                    $"Cannot set {nameof(Password)} on a server-attached database connection.");
            }

            _password = value;
        }
    }

    public bool ContainsProductionData
    {
        get => Server?.ContainsProductionData ?? _containsProductionData;
        set
        {
            if (Server != null)
            {
                throw new InvalidOperationException(
                    $"Cannot set {nameof(ContainsProductionData)} on a server-attached database connection.");
            }

            _containsProductionData = value;
        }
    }

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
    public string? ConnectionStringAugment
    {
        get => Server?.ConnectionStringAugment ?? _connectionStringAugment;
        set
        {
            if (Server != null)
            {
                throw new InvalidOperationException(
                    $"Cannot set {nameof(ConnectionStringAugment)} on a server-attached database connection.");
            }

            _connectionStringAugment = value;
        }
    }

    public void SetServer(DatabaseServerConnection? server)
    {
        Server = server;
        ServerId = server?.Id;
    }

    public abstract string GetConnectionString(IDataConnectionPasswordProtector passwordProtector);

    public abstract Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector);
}
