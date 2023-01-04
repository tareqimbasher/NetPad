using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NetPad.IO;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Configuration;

public class FileSystemSettingsRepository : ISettingsRepository
{
    private readonly FilePath _settingsFilePath;

    public FileSystemSettingsRepository()
    {
        _settingsFilePath = AppDataProvider.AppDataDirectoryPath.CombineFilePath("settings.json");
    }

    public Task<FilePath> GetSettingsFileLocationAsync()
    {
        return Task.FromResult(_settingsFilePath);
    }

    public async Task<Settings> GetSettingsAsync()
    {
        Settings settings;

        if (!_settingsFilePath.Exists())
        {
            settings = new Settings();
        }
        else
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath.Path).ConfigureAwait(false);

            // Validate settings file has a valid version
            var jsonRoot = JsonDocument.Parse(json).RootElement;
            if (!jsonRoot.TryGetProperty(nameof(Settings.Version).ToLower(), out var versionProp)
                || !Version.TryParse(versionProp.GetString(), out _))
            {
                settings = new Settings();
                await SaveSettingsAsync(settings);
            }

            settings = JsonSerializer.Deserialize<Settings>(json) ?? throw new Exception("Could not deserialize settings file.");

            if (settings.Upgrade())
            {
                await SaveSettingsAsync(settings);
            }
        }

        settings.DefaultMissingValues();

        return settings;
    }

    public async Task SaveSettingsAsync(Settings settings)
    {
        var json = JsonSerializer.Serialize(settings, true);
        await File.WriteAllTextAsync(_settingsFilePath.Path, json).ConfigureAwait(false);
    }
}
