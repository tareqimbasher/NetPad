using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

/// <summary>
/// Published after each start of a script's OmniSharp server, once the server has loaded the script's project.
/// Language features asked for before this was emitted got no answer, so clients ask for them again.
/// </summary>
public class OmniSharpServerReadyEvent(Guid scriptId) : IEvent
{
    public Guid ScriptId { get; } = scriptId;
}
