namespace NetPad.Data;

/// <summary>
/// Persists and retrieves data connections.
/// </summary>
public interface IDataConnectionRepository
{
    Task<IEnumerable<DataConnection>> GetAllAsync();
    Task<DataConnection?> GetAsync(Guid id);
    Task SaveAsync(DataConnection connection);
    Task DeleteAsync(Guid id);
}
