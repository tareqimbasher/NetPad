using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesGenerator
{
    Task<DataConnectionResources> GenerateResourcesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
}
