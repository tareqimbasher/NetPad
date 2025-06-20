using NetPad.DotNet;

namespace NetPad.Data.Metadata;

public interface IDataConnectionResourcesGenerator
{
    Task<DataConnectionResources> GenerateResourcesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
}
