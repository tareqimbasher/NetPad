using System.Threading.Tasks;

namespace OmniSharp
{
    internal interface IOmniSharpServerProcessAccessor<TEntry>
    {
        Task<TEntry> GetEntryPointAsync();
        Task StopProcessAsync();
    }
}
