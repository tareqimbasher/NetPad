namespace NetPad.Data;

/// <summary>
/// Represents a strategy that attempts to detect when a data connection's schema has changed.
/// </summary>
public interface IDataConnectionSchemaChangeDetectionStrategy
{
    /// <summary>
    /// Determines if the strategy can be used to read schema information for the specified data connection.
    /// </summary>
    bool CanSupport(DataConnection dataConnection);
    Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection);
    Task<SchemaCompareInfo?> GenerateSchemaCompareInfoAsync(DataConnection dataConnection);
}
