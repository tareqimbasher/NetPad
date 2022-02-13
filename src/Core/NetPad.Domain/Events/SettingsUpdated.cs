using NetPad.Configuration;

namespace NetPad.Events
{
    public class SettingsUpdated : IEvent
    {
        public SettingsUpdated(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; }
    }
}
