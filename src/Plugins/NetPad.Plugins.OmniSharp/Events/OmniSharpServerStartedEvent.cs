using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpServerStartedEvent : IEvent
{
    public OmniSharpServerStartedEvent(AppOmniSharpServer appOmniSharpServer)
    {
        AppOmniSharpServer = appOmniSharpServer;
    }

    public AppOmniSharpServer AppOmniSharpServer { get; }
}
