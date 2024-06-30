using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore;

internal class EntityFrameworkConnectionMetadataProviderFactory(IServiceProvider serviceProvider)
    : IDatabaseConnectionMetadataProviderFactory
{
    public IDatabaseConnectionMetadataProvider Create(DatabaseConnection databaseConnection)
    {
        if (databaseConnection is EntityFrameworkDatabaseConnection)
        {
            return serviceProvider.GetRequiredService<EntityFrameworkDatabaseConnectionMetadataProvider>();
        }

        throw new NotImplementedException("Only EntityFramework database connections are supported.");
    }
}
