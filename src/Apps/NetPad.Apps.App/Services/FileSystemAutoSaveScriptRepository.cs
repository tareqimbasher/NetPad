using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NetPad.Apps.Scripts;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Services;

/// <summary>
/// An implementation of <see cref="IAutoSaveScriptRepository"/> that persists auto-saved scripts
/// to the local file system.
/// </summary>
public class FileSystemAutoSaveScriptRepository : IAutoSaveScriptRepository
{
    private readonly Settings _settings;
    private readonly IScriptRepository _scriptRepository;
    private readonly IScriptSerializerFactory _serializerFactory;
    private readonly IScriptNameGenerator _scriptNameGenerator;
    private readonly IDataConnectionRepository _dataConnectionRepository;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<FileSystemAutoSaveScriptRepository> _logger;
    private readonly string _indexFilePath;
    private static readonly Lock _indexLock = new();

    public FileSystemAutoSaveScriptRepository(
        Settings settings,
        IScriptRepository scriptRepository,
        IScriptSerializerFactory serializerFactory,
        IScriptNameGenerator scriptNameGenerator,
        IDataConnectionRepository dataConnectionRepository,
        IDotNetInfo dotNetInfo,
        ILogger<FileSystemAutoSaveScriptRepository> logger)
    {
        _settings = settings;
        _scriptRepository = scriptRepository;
        _serializerFactory = serializerFactory;
        _scriptNameGenerator = scriptNameGenerator;
        _dataConnectionRepository = dataConnectionRepository;
        _dotNetInfo = dotNetInfo;
        _logger = logger;
        _indexFilePath = Path.Combine(GetRepositoryDirPath(), "index.json");
        Directory.CreateDirectory(_settings.AutoSaveScriptsDirectoryPath);
    }

    public async Task<Script?> GetScriptAsync(Guid scriptId)
    {
        var autoSavedScriptPath = FindAutoSavedScriptPath(scriptId);

        if (autoSavedScriptPath == null)
            return null;

        var repoScript = await _scriptRepository.GetAsync(scriptId);
        var scriptName = repoScript?.Name;

        if (scriptName == null && !GetIndex().TryGetValue(scriptId, out scriptName))
        {
            scriptName = _scriptNameGenerator.Generate();
        }

        var data = await File.ReadAllTextAsync(autoSavedScriptPath).ConfigureAwait(false);

        var script = await _serializerFactory.GetForPath(autoSavedScriptPath)
            .DeserializeAsync(scriptName, data, _dataConnectionRepository, _dotNetInfo);

        if (repoScript?.Path != null)
            script.SetPath(repoScript.Path);

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
                if (!_serializerFactory.IsKnownScriptPath(filePath))
                    continue;

                var fileNameWoExt = StripScriptExtension(filePath);

                if (!Guid.TryParse(fileNameWoExt, out var scriptId))
                    continue;

                var script = await GetScriptAsync(scriptId);
                if (script == null)
                    continue;

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
        var content = _serializerFactory.GetDefault().Serialize(script);
        await File.WriteAllTextAsync(scriptFilePath, content).ConfigureAwait(false);

        SaveToIndex(script.Id, script.Name);

        _logger.LogDebug("Auto-saved script: {Script}", script.ToString());

        return script;
    }

    public Task DeleteAsync(Script script)
    {
        var autoSavedScriptPath = FindAutoSavedScriptPath(script.Id);

        if (autoSavedScriptPath == null) return Task.CompletedTask;

        File.Delete(autoSavedScriptPath);
        DeleteFromIndex(script.Id);

        return Task.CompletedTask;
    }

    private string GetRepositoryDirPath() => _settings.AutoSaveScriptsDirectoryPath;

    private string GetAutoSavedScriptPath(Guid scriptId)
    {
        var ext = _serializerFactory.GetDefault().FileExtension;
        return Path.Combine(GetRepositoryDirPath(), $"{scriptId}{ext}");
    }

    private string? FindAutoSavedScriptPath(Guid scriptId)
    {
        foreach (var ext in _serializerFactory.GetAllFileExtensions())
        {
            var path = Path.Combine(GetRepositoryDirPath(), $"{scriptId}{ext}");
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private string StripScriptExtension(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        foreach (var ext in _serializerFactory.GetAllFileExtensions().OrderByDescending(e => e.Length))
        {
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return fileName[..^ext.Length];
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    private void SaveToIndex(Guid scriptId, string scriptName)
    {
        lock (_indexLock)
        {
            var map = GetIndex();
            map[scriptId] = scriptName;
            File.WriteAllText(_indexFilePath, JsonSerializer.Serialize(map, true));
        }
    }

    private void DeleteFromIndex(Guid scriptId)
    {
        lock (_indexLock)
        {
            var map = GetIndex();
            if (!map.Remove(scriptId))
                return;

            File.WriteAllText(_indexFilePath, JsonSerializer.Serialize(map, true));
        }
    }

    private Dictionary<Guid, string> GetIndex()
    {
        var map = File.Exists(_indexFilePath)
            ? JsonSerializer.Deserialize<Dictionary<Guid, string>>(File.ReadAllText(_indexFilePath))
            : new Dictionary<Guid, string>();

        return map ?? throw new Exception("Could not deserialize index file.");
    }
}
