using System;
using System.Threading.Tasks;

namespace NetPad.OmniSharpWrapper
{
    public interface IOmniSharpServerProcessAccessor<TEntry>
    {
        Task<TEntry> GetEntryPointAsync();
        Task StopProcessAsync();
    }
}