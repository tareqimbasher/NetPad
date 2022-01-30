using System;
using System.IO;
using System.Threading.Tasks;
using NetPad.Common;

namespace NetPad.Configuration
{
    public class FileSystemSettingsRepository : ISettingsRepository
    {
        private readonly string _settingsFilePath;

        public FileSystemSettingsRepository()
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NetPad",
                "settings.json");
        }

        public async Task<Settings> GetSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
                return new Settings();

            var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<Settings>(json) ?? throw new Exception("Could not get settings.");
        }

        public async Task SaveSettingsAsync(Settings settings)
        {
            var json = JsonSerializer.Serialize(settings, true);
            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);
        }
    }
}
