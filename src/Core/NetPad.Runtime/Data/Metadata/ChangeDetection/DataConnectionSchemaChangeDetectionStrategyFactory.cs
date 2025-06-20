using Microsoft.Extensions.DependencyInjection;

namespace NetPad.Data.Metadata.ChangeDetection;

public class DataConnectionSchemaChangeDetectionStrategyFactory(IServiceProvider serviceProvider)
    : IDataConnectionSchemaChangeDetectionStrategyFactory
{
    public IEnumerable<IDataConnectionSchemaChangeDetectionStrategy> GetStrategies(DataConnection dataConnection)
    {
        return serviceProvider.GetServices<IDataConnectionSchemaChangeDetectionStrategy>()
            .Where(s => s.CanSupport(dataConnection));
    }
}
