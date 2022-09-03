using System.Threading.Tasks;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Data;

public interface IDataConnectionResourcesGenerator
{
    Task<SourceCodeCollection> GenerateSourceCodeAsync(DataConnection dataConnection);
    Task<byte[]?> GenerateAssemblyAsync(DataConnection dataConnection, SourceCodeCollection sourceCode);
    Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection);
}
