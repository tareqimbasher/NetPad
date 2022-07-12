namespace NetPad.Plugins.OmniSharp.Services;

public class AppOmniSharpServerAccessor
{
    private AppOmniSharpServer? _appOmniSharpServer;

    public AppOmniSharpServer AppOmniSharpServer => _appOmniSharpServer
                                                    ?? throw new Exception($"{nameof(AppOmniSharpServer)} is not set.");

    public void Set(AppOmniSharpServer appOmniSharpServer)
    {
        _appOmniSharpServer = appOmniSharpServer;
    }
}
