using System;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Data;
using NetPad.Data.EntityFrameworkCore;
using NetPad.Data.EntityFrameworkCore.DataConnections;

namespace NetPad.Services.Data;

public class DatabaseConnectionMetadataProviderFactory : IDatabaseConnectionMetadataProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseConnectionMetadataProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDatabaseConnectionMetadataProvider Create(DatabaseConnection databaseConnection)
    {
        if (databaseConnection is EntityFrameworkDatabaseConnection)
        {
            return _serviceProvider.GetRequiredService<EntityFrameworkDatabaseConnectionMetadataProvider>();
        }

        throw new NotImplementedException("Only EntityFramework database connections are supported at this time.");
    }
}
