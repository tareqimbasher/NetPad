using NetPad.Events;

namespace NetPad.Configuration.Events;

public class SettingsUpdatedEvent(Settings settings) : IEvent
{
    public Settings Settings { get; } = settings;
}
