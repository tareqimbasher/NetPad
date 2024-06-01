using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesCache
{
    /// <summary>
    /// Determines if data connection has cached resources for the target .NET framework version.
    /// </summary>
    Task<bool> HasCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion);

    /// <summary>
    /// Removes all cached resources for data connection.
    /// </summary>
    Task RemoveCachedResourcesAsync(Guid dataConnectionId);

    /// <summary>
    /// Removes cached resources for data connection for the target .NET framework version.
    /// </summary>
    Task RemoveCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion);

    /// <summary>
    /// Gets compiled assembly needed to use connect and use data connection.
    /// </summary>
    Task<AssemblyImage?> GetAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);

    /// <summary>
    /// Gets generated source code needed to use data connection.
    /// </summary>
    Task<DataConnectionSourceCode> GetSourceGeneratedCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);

    /// <summary>
    /// Gets the references required to compile a working application for data connection.
    /// </summary>
    Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
}
