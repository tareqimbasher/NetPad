using System;

namespace NetPad.Data;

/// <summary>
/// Represents information needed to compare if a data connection's schema has changed.
/// </summary>
public abstract class SchemaCompareInfo
{
    protected SchemaCompareInfo(DateTime generatedAt)
    {
        GeneratedAt = generatedAt;
    }

    public DateTime GeneratedAt { get; init; }
}
