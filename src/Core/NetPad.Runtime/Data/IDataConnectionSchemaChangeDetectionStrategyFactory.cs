namespace NetPad.Data;

public interface IDataConnectionSchemaChangeDetectionStrategyFactory
{
    /// <summary>
    /// Gets which strategies can be used to detect if a data connection's schema has changed.
    /// </summary>
    IEnumerable<IDataConnectionSchemaChangeDetectionStrategy> GetStrategies(DataConnection dataConnection);
}
