using NetPad.DotNet;

namespace NetPad.Data.Metadata;

/// <summary>
/// A cache for generated data connection resources.
/// </summary>
public interface IDataConnectionResourcesCache
{
    /// <summary>
    /// Determines if data connection has cached resources for the target .NET framework version.
    /// </summary>
    Task<bool> HasCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion);

    /// <summary>
    /// Gets a list of <see cref="DotNetFrameworkVersion"/>s that have cached resources for a data connection.
    /// </summary>
    /// <param name="dataConnectionId"></param>
    /// <returns></returns>
    Task<IList<DotNetFrameworkVersion>> GetCachedDotNetFrameworkVersions(Guid dataConnectionId);

    /// <summary>
    /// Removes all cached resources for data connection.
    /// </summary>
    Task RemoveCachedResourcesAsync(Guid dataConnectionId);

    /// <summary>
    /// Removes cached resources for data connection for the target .NET framework version.
    /// </summary>
    Task RemoveCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion);

    /// <summary>
    /// Gets resource assets needed to use data connection on the target .NET framework.
    /// </summary>
    /// <param name="dataConnection"></param>
    /// <param name="targetFrameworkVersion"></param>
    /// <returns></returns>
    Task<DataConnectionResources> GetResourcesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
}
