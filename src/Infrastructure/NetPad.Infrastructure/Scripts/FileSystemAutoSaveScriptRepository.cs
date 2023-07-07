using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;

namespace NetPad.Scripts;

public class FileSystemAutoSaveScriptRepository : IAutoSaveScriptRepository
{
    private readonly Settings _settings;
    private readonly IScriptRepository _scriptRepository;
    private readonly IScriptNameGenerator _scriptNameGenerator;
    private readonly IDataConnectionRepository _dataConnectionRepository;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<FileSystemAutoSaveScriptRepository> _logger;
    private static readonly object _indexLock = new();

    public FileSystemAutoSaveScriptRepository(
        Settings settings,
        IScriptRepository scriptRepository,
        IScriptNameGenerator scriptNameGenerator,
        IDataConnectionRepository dataConnectionRepository,
        IDotNetInfo dotNetInfo,
        ILogger<FileSystemAutoSaveScriptRepository> logger)
    {
        _settings = settings;
        _scriptRepository = scriptRepository;
        _scriptNameGenerator = scriptNameGenerator;
        _dataConnectionRepository = dataConnectionRepository;
        _dotNetInfo = dotNetInfo;
        _logger = logger;
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

        if (scriptName == null && !GetIndex().TryGetValue(scriptId, out scriptName))
        {
            scriptName = _scriptNameGenerator.Generate();
        }

        var data = await File.ReadAllTextAsync(autoSavedScriptPath).ConfigureAwait(false);

        var script = await ScriptSerializer.DeserializeAsync(scriptName, data, _dataConnectionRepository, _dotNetInfo);
        if (repoScript?.Path != null)
            script.SetPath(repoScript.Path);

        if (script.Id != scriptId)
        {
            throw new Exception($"Auto-saved script on disk with ID: {script.Id} did not contain the same ID as indexed.");
        }

        script.IsDirty = true;

        return script;
    }

    public async Task<IEnumerable<Script>> GetScriptsAsync()
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

        SaveToIndex(script.Id, script.Name);

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

    private void SaveToIndex(Guid scriptId, string scriptName)
    {
        lock (_indexLock)
        {
            var map = GetIndex();

            if (map.ContainsKey(scriptId))
                map[scriptId] = scriptName;
            else
                map.Add(scriptId, scriptName);

            File.WriteAllText(GetIndexFilePath(), JsonSerializer.Serialize(map, true));
        }
    }

    private void DeleteFromIndex(Guid scriptId)
    {
        lock (_indexLock)
        {
            var map = GetIndex();

            if (!map.ContainsKey(scriptId)) return;

            map.Remove(scriptId);
            File.WriteAllText(GetIndexFilePath(), JsonSerializer.Serialize(map, true));
        }
    }

    private Dictionary<Guid, string> GetIndex()
    {
        var indexFilePath = GetIndexFilePath();

        var map = File.Exists(indexFilePath)
            ? JsonSerializer.Deserialize<Dictionary<Guid, string>>(File.ReadAllText(indexFilePath))
            : new Dictionary<Guid, string>();

        return map ?? throw new Exception("Could not deserialize index file.");
    }

    private string GetIndexFilePath()
    {
        return Path.Combine(GetRepositoryDirPath(), "index.json");
    }
}
