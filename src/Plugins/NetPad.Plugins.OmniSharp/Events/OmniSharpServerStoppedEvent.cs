using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpServerStoppedEvent : IEvent
{
    public OmniSharpServerStoppedEvent(AppOmniSharpServer appOmniSharpServer)
    {
        AppOmniSharpServer = appOmniSharpServer;
    }

    public AppOmniSharpServer AppOmniSharpServer { get; }
}
