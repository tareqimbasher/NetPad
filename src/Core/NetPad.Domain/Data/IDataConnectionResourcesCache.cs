using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Data;

public interface IDataConnectionResourcesCache
{
    Task<byte[]?> GetAssemblyAsync(DataConnection dataConnection);
    Task<SourceCodeCollection> GetSourceGeneratedCodeAsync(DataConnection dataConnection);
    Task<IEnumerable<Reference>> GetRequiredReferencesAsync(DataConnection dataConnection);
}
