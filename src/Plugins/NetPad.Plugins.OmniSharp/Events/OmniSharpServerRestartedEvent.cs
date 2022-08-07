using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpServerRestartedEvent : IEvent
{
    public OmniSharpServerRestartedEvent(AppOmniSharpServer appOmniSharpServer)
    {
        AppOmniSharpServer = appOmniSharpServer;
    }

    public AppOmniSharpServer AppOmniSharpServer { get; }
}
