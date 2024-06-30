using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpServerRestartedEvent(AppOmniSharpServer appOmniSharpServer) : IEvent
{
    public AppOmniSharpServer AppOmniSharpServer { get; } = appOmniSharpServer;
}
