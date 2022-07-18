using NetPad.Configuration;

namespace NetPad.Events;

public class SettingsUpdatedEvent : IEvent
{
    public SettingsUpdatedEvent(Settings settings)
    {
        Settings = settings;
    }

    public Settings Settings { get; }
}
