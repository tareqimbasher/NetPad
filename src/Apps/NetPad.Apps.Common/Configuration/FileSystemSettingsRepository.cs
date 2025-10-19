using System.Text.Json;
using NetPad.Configuration;
using NetPad.IO;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.Configuration;

/// <summary>
/// An implementation of <see cref="ISettingsRepository"/> that persists settings to the local file system.
/// </summary>
public class FileSystemSettingsRepository : ISettingsRepository
{
    public Task<FilePath> GetSettingsFileLocationAsync()
    {
        return Task.FromResult(AppDataProvider.SettingsFilePath);
    }

    public async Task<Settings> GetSettingsAsync()
    {
        Settings settings;

        if (!AppDataProvider.SettingsFilePath.Exists())
        {
            settings = new Settings();
        }
        else
        {
            var json = await File.ReadAllTextAsync(AppDataProvider.SettingsFilePath.Path).ConfigureAwait(false);

            // Validate settings file has a valid version
            var jsonRoot = JsonDocument.Parse(json).RootElement;
            if (!jsonRoot.TryGetProperty(nameof(Settings.Version).ToLowerInvariant(), out var versionProp)
                || !Version.TryParse(versionProp.GetString(), out _))
            {
                settings = new Settings();
                await SaveSettingsAsync(settings);
            }

            settings = JsonSerializer.Deserialize<Settings>(json) ??
                       throw new Exception("Could not deserialize settings file.");
        }

        settings.DefaultMissingValues();

        if (settings.Upgrade())
        {
            await SaveSettingsAsync(settings, true);
        }

        return settings;
    }

    public async Task SaveSettingsAsync(Settings settings, bool backupOld = false)
    {
        if (backupOld)
        {
            File.Copy(AppDataProvider.SettingsFilePath.Path, Path.Combine(
                    Path.GetDirectoryName(AppDataProvider.SettingsFilePath.Path)!,
                    "Backups",
                    $"{Path.GetFileNameWithoutExtension(AppDataProvider.SettingsFilePath.Path)}_backup_{DateTime.Now:s}",
                    Path.GetExtension(AppDataProvider.SettingsFilePath.Path)),
                false);
        }

        var json = JsonSerializer.Serialize(settings, true);
        await File.WriteAllTextAsync(AppDataProvider.SettingsFilePath.Path, json).ConfigureAwait(false);
    }
}
