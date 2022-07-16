namespace NetPad.Plugins.OmniSharp.Services;

public interface IOmniSharpServerLocator
{
    public Task<OmniSharpServerLocation?> GetServerLocationAsync();
}
