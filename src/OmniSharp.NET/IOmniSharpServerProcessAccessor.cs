using System.Threading.Tasks;

namespace OmniSharp
{
    public interface IOmniSharpServerProcessAccessor<TEntry>
    {
        Task<TEntry> GetEntryPointAsync();
        Task StopProcessAsync();
    }
}