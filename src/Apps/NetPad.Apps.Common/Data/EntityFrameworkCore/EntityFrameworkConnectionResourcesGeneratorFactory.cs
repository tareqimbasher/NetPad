using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore;

internal class EntityFrameworkConnectionResourcesGeneratorFactory(IServiceProvider serviceProvider)
    : IDataConnectionResourcesGeneratorFactory
{
    public IDataConnectionResourcesGenerator Create(DataConnection dataConnection)
    {
        if (dataConnection is EntityFrameworkDatabaseConnection)
        {
            return serviceProvider.GetRequiredService<EntityFrameworkResourcesGenerator>();
        }

        throw new NotImplementedException("Only EntityFramework data connections are supported.");
    }
}
