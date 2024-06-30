using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpServerStartedEvent(AppOmniSharpServer appOmniSharpServer) : IEvent
{
    public AppOmniSharpServer AppOmniSharpServer { get; } = appOmniSharpServer;
}
