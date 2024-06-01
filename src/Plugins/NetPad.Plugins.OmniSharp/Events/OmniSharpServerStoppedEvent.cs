using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpServerStoppedEvent(AppOmniSharpServer appOmniSharpServer) : IEvent
{
    public AppOmniSharpServer AppOmniSharpServer { get; } = appOmniSharpServer;
}
