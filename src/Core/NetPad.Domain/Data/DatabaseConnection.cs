using System;
using Microsoft.AspNetCore.DataProtection;

namespace NetPad.Data;

public abstract class DatabaseConnection : DataConnection
{
    protected DatabaseConnection(Guid id, string name, DataConnectionType type) : base(id, name, type)
    {
    }

    public string? Host { get; set; }
    public string? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public bool ContainsProductionData { get; set; }

    public abstract string GetConnectionString(IDataConnectionPasswordProtector passwordProtector);
}
