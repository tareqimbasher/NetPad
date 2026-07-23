using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using NetPad.Apps.Configuration.SettingsFiles;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.IO;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.Configuration;

/// <summary>
/// An implementation of <see cref="ISettingsRepository"/> that persists settings to the local file system.
/// </summary>
public class FileSystemSettingsRepository(
    ILogger<FileSystemSettingsRepository> logger,
    FilePath? settingsFilePath = null) : ISettingsRepository
{
    private readonly FilePath _settingsFilePath = settingsFilePath ?? AppDataProvider.SettingsFilePath;
    private readonly JsonMigrationPipeline _migrationPipeline = new([new SettingsFileV0ToV1MigrationStep()]);

    public Task<FilePath> GetSettingsFileLocationAsync()
    {
        return Task.FromResult(_settingsFilePath);
    }

    public async Task<Settings> GetSettingsAsync()
    {
        Settings settings;
        bool save = false;
        bool backedUp = false;

        if (!_settingsFilePath.Exists())
        {
            settings = new Settings();
        }
        else
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath.Path).ConfigureAwait(false);
            var doc = TryParse(json);

            if (doc == null)
            {
                // A file we cannot parse is not a file we can merge with. The user's copy is kept so
                // whatever is salvageable in it is not lost.
                var backupPath = BackupSettingsFile();
                logger.LogError(
                    "Settings file could not be parsed and was reset to defaults. The original was backed up to: {BackupPath}",
                    backupPath?.Path);

                settings = new Settings();
                save = true;
                backedUp = true;
            }
            else
            {
                int version = NormalizeVersion(doc);

                if (version > _migrationPipeline.LatestVersion)
                {
                    throw new InvalidOperationException(
                        $"Settings file '{_settingsFilePath.Path}' is version {version}, which was written by a " +
                        $"newer version of NetPad. The newest version this build understands is " +
                        $"{_migrationPipeline.LatestVersion}.");
                }

                if (version < _migrationPipeline.LatestVersion)
                {
                    var backupPath = BackupSettingsFile();
                    logger.LogInformation(
                        "Migrating settings file from version {FromVersion} to {ToVersion}. The original was backed up to: {BackupPath}",
                        version,
                        _migrationPipeline.LatestVersion,
                        backupPath?.Path);

                    settings = _migrationPipeline.MigrateToLatest<Settings>(doc.ToJsonString(), JsonSerializer.DefaultOptions);
                    save = true;
                    backedUp = true;
                }
                else
                {
                    settings = JsonSerializer.Deserialize<Settings>(json) ??
                               throw new Exception("Could not deserialize settings file.");
                }
            }
        }

        settings.DefaultMissingValues();

        if (settings.CheckScriptsDirectory())
        {
            save = true;
        }

        if (save)
        {
            await SaveSettingsAsync(settings, !backedUp);
        }

        return settings;
    }

    public async Task SaveSettingsAsync(Settings settings, bool backupOld = false)
    {
        if (backupOld)
        {
            BackupSettingsFile();
        }

        var json = JsonSerializer.Serialize(settings, true);
        await File.WriteAllTextAsync(_settingsFilePath.Path, json).ConfigureAwait(false);
    }

    private static JsonObject? TryParse(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Rewrites the document's version node to the version the migration pipeline expects, and returns
    /// it. Settings files that predate schema versioning carry a product-style version string ("1.0")
    /// or no version at all; both are version 0.
    /// </summary>
    private static int NormalizeVersion(JsonObject doc)
    {
        if (doc.TryGetPropertyValue("version", out var node)
            && node is JsonValue value
            && value.TryGetValue<int>(out var version))
        {
            return version;
        }

        doc["version"] = 0;
        return 0;
    }

    /// <summary>
    /// Copies the settings file to the Backups directory. Returns the backup's path, or
    /// <see langword="null"/> if there was no file to back up.
    /// </summary>
    private FilePath? BackupSettingsFile()
    {
        if (!_settingsFilePath.Exists())
        {
            return null;
        }

        var backupDirectory = Path.Combine(Path.GetDirectoryName(_settingsFilePath.Path)!, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var backupPath = new FilePath(Path.Combine(
            backupDirectory,
            $"{_settingsFilePath.FileNameWithoutExtension}_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{_settingsFilePath.Extension}"));

        File.Copy(_settingsFilePath.Path, backupPath.Path, true);

        return backupPath;
    }
}
