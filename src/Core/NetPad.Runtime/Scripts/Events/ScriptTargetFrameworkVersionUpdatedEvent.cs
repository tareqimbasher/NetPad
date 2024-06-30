using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptTargetFrameworkVersionUpdatedEvent(
    Script script,
    DotNetFrameworkVersion oldVersion,
    DotNetFrameworkVersion newVersion)
    : IEvent
{
    public Script Script { get; } = script;
    public DotNetFrameworkVersion OldVersion { get; } = oldVersion;
    public DotNetFrameworkVersion NewVersion { get; } = newVersion;
}
