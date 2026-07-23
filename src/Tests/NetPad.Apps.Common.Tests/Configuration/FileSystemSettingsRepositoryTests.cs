using Microsoft.Extensions.Logging.Abstractions;
using NetPad.Apps.Configuration;
using NetPad.Configuration;
using NetPad.IO;

namespace NetPad.Apps.Common.Tests.Configuration;

public sealed class FileSystemSettingsRepositoryTests : IDisposable
{
    private readonly DirectoryPath _directory;
    private readonly FilePath _settingsFile;

    public FileSystemSettingsRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "NetPadTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory.Path);
        _settingsFile = _directory.CombineFilePath("settings.json");
    }

    public void Dispose()
    {
        Directory.Delete(_directory.Path, true);
    }

    [Fact]
    public async Task Migrates_A_V0_File_Backing_Up_The_Original()
    {
        var original = V0File("Dark");
        await File.WriteAllTextAsync(_settingsFile.Path, original);

        var settings = await CreateRepository().GetSettingsAsync();

        Assert.Equal(ThemeMode.Dark, settings.Appearance.Mode);

        // Deliberately not the default: this has to fail if the fold silently did nothing.
        Assert.Equal(StatusIndicatorVisibility.WhileRunning, settings.Appearance.ScriptRunStatusIndicatorInExplorer);

        var onDisk = await File.ReadAllTextAsync(_settingsFile.Path);
        Assert.Contains("\"version\": 1", onDisk);
        Assert.DoesNotContain("\"theme\"", onDisk);
        Assert.DoesNotContain("iconTheme", onDisk);
        Assert.DoesNotContain("IndicatorInScriptsList", onDisk);
        Assert.DoesNotContain("\"font\"", onDisk);

        var backup = Assert.Single(Backups());
        Assert.Equal(original, await File.ReadAllTextAsync(backup));
        Assert.DoesNotContain(':', Path.GetFileName(backup));
    }

    [Fact]
    public async Task A_Migrated_File_Is_Not_Touched_Again_On_The_Next_Load()
    {
        await File.WriteAllTextAsync(_settingsFile.Path, V0File("Light"));
        await CreateRepository().GetSettingsAsync();

        var afterMigration = await File.ReadAllBytesAsync(_settingsFile.Path);

        var settings = await CreateRepository().GetSettingsAsync();

        Assert.Equal(ThemeMode.Light, settings.Appearance.Mode);
        Assert.Equal(afterMigration, await File.ReadAllBytesAsync(_settingsFile.Path));
        Assert.Single(Backups());
    }

    [Fact]
    public async Task An_Unparseable_File_Is_Backed_Up_And_Reset_To_Defaults()
    {
        const string corrupt = "{ this is not json";
        await File.WriteAllTextAsync(_settingsFile.Path, corrupt);

        var settings = await CreateRepository().GetSettingsAsync();

        Assert.Equal(ThemeMode.System, settings.Appearance.Mode);

        var backup = Assert.Single(Backups());
        Assert.Equal(corrupt, await File.ReadAllTextAsync(backup));

        // The corrupt file is replaced, so the next load is not a second reset.
        var onDisk = await File.ReadAllTextAsync(_settingsFile.Path);
        Assert.Contains("\"version\": 1", onDisk);
    }

    [Fact]
    public async Task A_File_Written_By_A_Newer_NetPad_Throws()
    {
        await File.WriteAllTextAsync(_settingsFile.Path, """{"version": 2}""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateRepository().GetSettingsAsync());

        Assert.Contains("newer version of NetPad", exception.Message);
        Assert.Empty(Backups());
    }

    private FileSystemSettingsRepository CreateRepository() =>
        new(NullLogger<FileSystemSettingsRepository>.Instance, _settingsFile);

    private string[] Backups()
    {
        var backupDirectory = _directory.Combine("Backups");
        return backupDirectory.Exists() ? Directory.GetFiles(backupDirectory.Path) : [];
    }

    /// <summary>
    /// A settings file in the shape NetPad wrote before the settings file was versioned, with its
    /// directories pointed inside the test's temp directory so loading it touches nothing else.
    /// </summary>
    private string V0File(string theme) =>
        $$"""
          {
            "version": "1.0",
            "scriptsDirectoryPath": {{Quote(_directory.Combine("Scripts").Path)}},
            "autoSaveScriptsDirectoryPath": {{Quote(_directory.Combine("AutoSave").Path)}},
            "packageCacheDirectoryPath": {{Quote(_directory.Combine("Packages").Path)}},
            "appearance": {
              "theme": "{{theme}}",
              "iconTheme": "Colorful",
              "showScriptRunStatusIndicatorInScriptsList": false,
              "showScriptRunningIndicatorInScriptsList": true
            },
            "results": {
              "font": "monospace"
            }
          }
          """;

    private static string Quote(string value) => NetPad.Common.JsonSerializer.Serialize(value);
}
