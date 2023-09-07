using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace NetPad.Data;

public class DataConnectionSchemaChangeDetectionStrategyFactory : IDataConnectionSchemaChangeDetectionStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataConnectionSchemaChangeDetectionStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<IDataConnectionSchemaChangeDetectionStrategy> GetStrategies(DataConnection dataConnection)
    {
        return _serviceProvider.GetServices<IDataConnectionSchemaChangeDetectionStrategy>()
            .Where(s => s.CanSupport(dataConnection));
    }
}
