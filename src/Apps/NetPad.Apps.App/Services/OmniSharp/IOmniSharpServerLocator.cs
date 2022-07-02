using System.Threading.Tasks;

namespace NetPad.Services.OmniSharp;

public interface IOmniSharpServerLocator
{
    public Task<OmniSharpServerLocation?> GetServerLocationAsync();
}
