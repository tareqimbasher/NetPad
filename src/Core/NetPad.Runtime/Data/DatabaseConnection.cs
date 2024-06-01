namespace NetPad.Data;

public abstract class DatabaseConnection(Guid id, string name, DataConnectionType type) : DataConnection(id, name, type)
{
    public string? Host { get; set; }
    public string? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public bool ContainsProductionData { get; set; }
    public string? ConnectionStringAugment { get; set; }

    public abstract string GetConnectionString(IDataConnectionPasswordProtector passwordProtector);
}
