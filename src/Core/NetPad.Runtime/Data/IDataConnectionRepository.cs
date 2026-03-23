namespace NetPad.Data;

/// <summary>
/// Persists and retrieves data connections and database server connections.
/// </summary>
public interface IDataConnectionRepository
{
    Task<IEnumerable<DataConnection>> GetAllAsync();
    Task<DataConnection?> GetAsync(Guid id);
    Task SaveAsync(DataConnection connection);
    Task DeleteAsync(Guid id);

    Task<IEnumerable<DatabaseServerConnection>> GetAllServersAsync();
    Task<DatabaseServerConnection?> GetServerAsync(Guid id);
    Task SaveServerAsync(DatabaseServerConnection server);
    Task DeleteServerAsync(Guid id);
}
