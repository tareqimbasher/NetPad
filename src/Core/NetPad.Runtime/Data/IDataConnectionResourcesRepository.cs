using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesRepository
{
    Task<bool> HasResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion);
    Task<IList<DotNetFrameworkVersion>> GetCachedDotNetFrameworkVersionsAsync(Guid dataConnectionId);
    Task<DataConnectionResources?> GetAsync(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion);
    Task SaveAsync(DataConnectionResources resources, DotNetFrameworkVersion dotNetFrameworkVersion);
    Task DeleteAsync(Guid dataConnectionId);
    Task DeleteAsync(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion);

    Task<TSchemaCompareInfo?> GetSchemaCompareInfoAsync<TSchemaCompareInfo>(Guid dataConnectionId) where TSchemaCompareInfo : SchemaCompareInfo;
    Task SaveSchemaCompareInfoAsync(Guid dataConnectionId, SchemaCompareInfo compareInfo);
    Task DeleteSchemaCompareInfoAsync(Guid dataConnectionId);
}
