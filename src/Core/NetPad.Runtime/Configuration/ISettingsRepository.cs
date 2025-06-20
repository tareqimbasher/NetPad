using NetPad.IO;

namespace NetPad.Configuration;

/// <summary>
/// Persists and reads settings.
/// </summary>
public interface ISettingsRepository
{
    Task<FilePath> GetSettingsFileLocationAsync();
    Task<Settings> GetSettingsAsync();
    Task SaveSettingsAsync(Settings settings, bool backupOld = false);
}
