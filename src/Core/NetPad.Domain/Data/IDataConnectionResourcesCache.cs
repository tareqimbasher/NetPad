using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Compilation;
using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesCache
{
    bool HasCachedResources(DataConnection dataConnection);
    Task<byte[]?> GetAssemblyAsync(DataConnection dataConnection);
    Task<SourceCodeCollection> GetSourceGeneratedCodeAsync(DataConnection dataConnection);
    Task<IEnumerable<Reference>> GetRequiredReferencesAsync(DataConnection dataConnection);
}
