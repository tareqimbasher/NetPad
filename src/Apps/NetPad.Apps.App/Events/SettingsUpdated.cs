using NetPad.Configuration;

namespace NetPad.Events
{
    public class SettingsUpdated
    {
        public SettingsUpdated(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; }
    }
}
