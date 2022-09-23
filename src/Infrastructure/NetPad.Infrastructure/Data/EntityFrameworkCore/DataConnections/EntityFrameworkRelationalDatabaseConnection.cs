using System;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkRelationalDatabaseConnection : EntityFrameworkDatabaseConnection
{
    protected EntityFrameworkRelationalDatabaseConnection(Guid id, string name, DataConnectionType type, string entityFrameworkProviderName)
        : base(id, name, type, entityFrameworkProviderName)
    {
    }
}
