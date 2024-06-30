using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesGenerator
{
    Task<DataConnectionSourceCode> GenerateSourceCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
    Task<AssemblyImage?> GenerateAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
    Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
}
