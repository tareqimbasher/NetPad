using System;
using NetPad.Common;

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
    public string? GeneratedOnAppVersion { get; init; }

    public bool GeneratedUsingStaleAppVersion()
    {
        if (GeneratedOnAppVersion == null
            || !SemanticVersion.TryParse(GeneratedOnAppVersion, out var infoAppVersion)
            || infoAppVersion < GlobalConsts.DataConnectionCacheValidOnOrAfterAppVersion)
        {
            return true;
        }

        return false;
    }
}
