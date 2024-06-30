using Microsoft.Extensions.DependencyInjection;

namespace NetPad.Data;

public class DataConnectionSchemaChangeDetectionStrategyFactory(IServiceProvider serviceProvider)
    : IDataConnectionSchemaChangeDetectionStrategyFactory
{
    public IEnumerable<IDataConnectionSchemaChangeDetectionStrategy> GetStrategies(DataConnection dataConnection)
    {
        return serviceProvider.GetServices<IDataConnectionSchemaChangeDetectionStrategy>()
            .Where(s => s.CanSupport(dataConnection));
    }
}
