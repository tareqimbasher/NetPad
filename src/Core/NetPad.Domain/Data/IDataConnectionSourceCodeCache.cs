using System.Threading.Tasks;
using NetPad.Compilation;

namespace NetPad.Data;

public interface IDataConnectionSourceCodeCache
{
    public Task<SourceCodeCollection> GetSourceGeneratedCodeAsync(DataConnection dataConnection);
}
