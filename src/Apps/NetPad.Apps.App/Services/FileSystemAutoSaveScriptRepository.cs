using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Apps.Scripts;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;
using NetPad.Services.AutoSaveFiles;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Services;

/// <summary>
/// An implementation of <see cref="IAutoSaveScriptRepository"/> that persists auto-saved scripts
/// to the local file system.
/// </summary>
public class FileSystemAutoSaveScriptRepository : IAutoSaveScriptRepository
{
    private readonly Settings _settings;
    private readonly IScriptRepository _scriptRepository;
    private readonly IScriptNameGenerator _scriptNameGenerator;
    private readonly IDataConnectionRepository _dataConnectionRepository;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;
    private readonly ILogger<FileSystemAutoSaveScriptRepository> _logger;
    private readonly string _indexFilePath;

    private readonly JsonMigrationPipeline _indexFileMigrationPipeline =
        new([new AutoSaveIndexFileV0ToV1MigrationStep()]);

    private static readonly Lock _indexLock = new();

    public FileSystemAutoSaveScriptRepository(
        Settings settings,
        IScriptRepository scriptRepository,
        IScriptNameGenerator scriptNameGenerator,
        IDataConnectionRepository dataConnectionRepository,
        IDotNetInfo dotNetInfo,
        IAppStatusMessagePublisher appStatusMessagePublisher,
        ILogger<FileSystemAutoSaveScriptRepository> logger)
    {
        _settings = settings;
        _scriptRepository = scriptRepository;
        _scriptNameGenerator = scriptNameGenerator;
        _dataConnectionRepository = dataConnectionRepository;
        _dotNetInfo = dotNetInfo;
        _appStatusMessagePublisher = appStatusMessagePublisher;
        _logger = logger;
        _indexFilePath = Path.Combine(GetRepositoryDirPath(), "index.json");
        Directory.CreateDirectory(_settings.AutoSaveScriptsDirectoryPath);
    }

    public async Task<Script?> GetScriptAsync(Guid scriptId)
    {
        var autoSavedScriptPath = GetAutoSavedScriptPath(scriptId);

        if (!File.Exists(autoSavedScriptPath))
            return null;

        // If this is a script saved in repo, use its latest name in case it has changed
        var repoScript = await _scriptRepository.GetAsync(scriptId);
        var scriptName = repoScript?.Name;

        AutoSaveIndexEntry? indexEntry = null;
        if (scriptName == null)
        {
            var index = GetIndex();
            if (index.Entries.TryGetValue(scriptId, out var entry))
            {
                indexEntry = entry;
                scriptName = entry.Name;
            }
        }

        if (string.IsNullOrWhiteSpace(scriptName))
        {
            scriptName = _scriptNameGenerator.Generate();
        }

        var data = await File.ReadAllTextAsync(autoSavedScriptPath).ConfigureAwait(false);

        var script = await ScriptSerializer.DeserializeAsync(scriptName, data, _dataConnectionRepository, _dotNetInfo);

        if (repoScript?.Path != null)
        {
            script.SetPath(repoScript.Path);
        }
        else if (indexEntry?.OriginalPath != null)
        {
            if (File.Exists(indexEntry.OriginalPath))
            {
                script.SetPath(indexEntry.OriginalPath);
            }
            else
            {
                _ = _appStatusMessagePublisher.PublishAsync(
                    script.Id,
                    $"Recovered unsaved changes from {indexEntry.OriginalPath} — original file is missing. Save to keep these changes.",
                    AppStatusMessagePriority.High);
            }
        }

        if (script.Id != scriptId)
        {
            throw new Exception(
                $"Auto-saved script on disk with ID: {script.Id} did not contain the same ID as indexed.");
        }

        script.IsDirty = true;

        return script;
    }

    public async Task<List<Script>> GetScriptsAsync()
    {
        var scripts = new List<Script>();

        foreach (var filePath in Directory.GetFiles(GetRepositoryDirPath()))
        {
            try
            {
                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(filePath), out var scriptId))
                {
                    continue;
                }

                var script = await GetScriptAsync(scriptId);
                if (script == null)
                {
                    continue;
                }

                scripts.Add(script);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load script at path: {ScriptPath}", filePath);
            }
        }

        return scripts;
    }

    public async Task<Script> SaveAsync(Script script)
    {
        var scriptFilePath = GetAutoSavedScriptPath(script.Id);

        await File.WriteAllTextAsync(scriptFilePath, ScriptSerializer.Serialize(script)).ConfigureAwait(false);

        SaveToIndex(script.Id, script.Name, script.Path);

        _logger.LogDebug("Auto-saved script: {Script}", script.ToString());

        return script;
    }

    public Task DeleteAsync(Script script)
    {
        var autoSavedScriptPath = GetAutoSavedScriptPath(script.Id);

        if (!File.Exists(autoSavedScriptPath)) return Task.CompletedTask;

        File.Delete(autoSavedScriptPath);

        DeleteFromIndex(script.Id);

        return Task.CompletedTask;
    }

    private string GetRepositoryDirPath()
    {
        return _settings.AutoSaveScriptsDirectoryPath;
    }

    private string GetAutoSavedScriptPath(Guid scriptId)
    {
        return Path.Combine(GetRepositoryDirPath(), $"{scriptId}.{Script.STANDARD_EXTENSION_WO_DOT}");
    }

    private void SaveToIndex(Guid scriptId, string scriptName, string? originalPath)
    {
        lock (_indexLock)
        {
            var index = GetIndex();
            index.Entries[scriptId] = new AutoSaveIndexEntry(scriptName, originalPath);
            File.WriteAllText(_indexFilePath, JsonSerializer.Serialize(index, true));
        }
    }

    private void DeleteFromIndex(Guid scriptId)
    {
        lock (_indexLock)
        {
            var index = GetIndex();
            if (!index.Entries.Remove(scriptId))
            {
                return;
            }

            File.WriteAllText(_indexFilePath, JsonSerializer.Serialize(index, true));
        }
    }

    private AutoSaveIndexFileV1 GetIndex()
    {
        lock (_indexLock)
        {
            if (!File.Exists(_indexFilePath))
            {
                return new AutoSaveIndexFileV1();
            }

            var json = File.ReadAllText(_indexFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new AutoSaveIndexFileV1();
            }

            return _indexFileMigrationPipeline.MigrateToLatest<AutoSaveIndexFileV1>(json,
                JsonSerializer.DefaultOptions);
        }
    }
}
