using System;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Data;
using NetPad.Data.EntityFrameworkCore;
using NetPad.Data.EntityFrameworkCore.DataConnections;

namespace NetPad.Services.Data;

public class DataConnectionResourcesGeneratorFactory : IDataConnectionResourcesGeneratorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataConnectionResourcesGeneratorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDataConnectionResourcesGenerator Create(DataConnection dataConnection)
    {
        if (dataConnection is EntityFrameworkDatabaseConnection)
        {
            return _serviceProvider.GetRequiredService<EntityFrameworkResourcesGenerator>();
        }

        throw new NotImplementedException("Only EntityFramework data connections are supported at this time.");
    }
}
