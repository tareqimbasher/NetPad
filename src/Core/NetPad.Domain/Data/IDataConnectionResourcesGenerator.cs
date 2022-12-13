using System.Threading.Tasks;
using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesGenerator
{
    Task<DataConnectionSourceCode> GenerateSourceCodeAsync(DataConnection dataConnection);
    Task<AssemblyImage?> GenerateAssemblyAsync(DataConnection dataConnection);
    Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection);
}
