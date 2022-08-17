using System.Threading.Tasks;

namespace NetPad.Data;

public interface IDataConnectionAssemblyCache
{
    Task<byte[]?> GetAssemblyAsync(DataConnection dataConnection);
}

