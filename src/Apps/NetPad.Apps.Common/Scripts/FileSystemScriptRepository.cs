using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Exceptions;
using NetPad.Scripts;

namespace NetPad.Apps.Scripts;

/// <summary>
/// An implementation of <see cref="IScriptRepository"/> that persists scripts to the local file system.
/// </summary>
public class FileSystemScriptRepository : IScriptRepository
{
    private readonly Settings _settings;
    private readonly IDataConnectionRepository _dataConnectionRepository;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<FileSystemScriptRepository> _logger;

    public FileSystemScriptRepository(
        Settings settings,
        IDataConnectionRepository dataConnectionRepository,
        IDotNetInfo dotNetInfo,
        ILogger<FileSystemScriptRepository> logger)
    {
        _settings = settings;
        _dataConnectionRepository = dataConnectionRepository;
        _dotNetInfo = dotNetInfo;
        _logger = logger;
        Directory.CreateDirectory(GetRepositoryDirPath());
    }

    public async Task<IList<Script>> GetAllAsync()
    {
        var scripts = new ConcurrentBag<Script>();

        await Parallel.ForEachAsync(
            EnumerateScriptFiles(),
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (scriptFile, _) =>
            {
                try
                {
                    var script = await GetAsync(scriptFile);
                    scripts.Add(script);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to load script: {FilePath}", scriptFile);
                }
            });

        return [.. scripts];
    }

    public Task<IList<ScriptSummary>> GetSummariesAsync()
    {
        var summaries = new List<ScriptSummary>();

        foreach (var scriptFile in EnumerateScriptFiles())
        {
            var scriptId = ReadScriptId(scriptFile);
            if (scriptId == null) continue;

            ScriptSerializer.ScriptData? scriptData = null;
            using (var sr = File.OpenText(scriptFile))
            {
                // Skip first line (script ID)
                sr.ReadLine();
                if (sr.ReadLine() is { } secondLine)
                {
                    scriptData = ScriptSerializer.DeserializeScriptData(secondLine);
                }
            }

            if (scriptData == null)
            {
                continue;
            }

            summaries.Add(new ScriptSummary(
                scriptId.Value,
                Path.GetFileNameWithoutExtension(scriptFile),
                scriptFile.Replace(Path.PathSeparator, '/'),
                scriptData.Config.Kind ?? ScriptKind.Program,
                scriptData.Config.TargetFrameworkVersion ?? scriptData.Config.ToScriptConfig(_dotNetInfo).TargetFrameworkVersion,
                scriptData.DataConnection?.Id));
        }

        return Task.FromResult<IList<ScriptSummary>>(summaries);
    }

    public Task<Script> CreateAsync(string name, DotNetFrameworkVersion targetFrameworkVersion)
    {
        var script = new Script(
            ScriptIdGenerator.NewId(),
            name,
            new ScriptConfig(ScriptKind.Program, targetFrameworkVersion));

        return Task.FromResult(script);
    }

    public async Task<Script> GetAsync(string path)
    {
        // Basic protection against malicious calls
        if (!path.EndsWithIgnoreCase(Script.STANDARD_EXTENSION))
            throw new InvalidOperationException($"Script file must end with {Script.STANDARD_EXTENSION}");

        var fileInfo = new FileInfo(path);

        if (!fileInfo.Exists)
            throw new ScriptNotFoundException(path);

        var data = await File.ReadAllTextAsync(path).ConfigureAwait(false);

        var name = Script.GetNameFromPath(path);
        var script = await ScriptSerializer.DeserializeAsync(name, data, _dataConnectionRepository, _dotNetInfo);
        script.SetPath(path);

        return script;
    }

    public async Task<Script?> GetAsync(Guid scriptId)
    {
        foreach (var scriptFile in EnumerateScriptFiles())
        {
            if (ReadScriptId(scriptFile) != scriptId) continue;
            return await GetAsync(scriptFile);
        }

        return null;
    }

    public async Task<IList<Script>> GetAsync(HashSet<Guid> scriptIds)
    {
        var scripts = new List<Script>();
        var remaining = new HashSet<Guid>(scriptIds);

        foreach (var scriptFile in EnumerateScriptFiles())
        {
            if (remaining.Count == 0)
            {
                break;
            }

            var scriptId = ReadScriptId(scriptFile);
            if (scriptId == null || !remaining.Remove(scriptId.Value))
            {
                continue;
            }

            try
            {
                var script = await GetAsync(scriptFile);
                scripts.Add(script);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load script: {FilePath}", scriptFile);
            }
        }

        return scripts;
    }

    public async Task<Script> SaveAsync(Script script)
    {
        if (script.Path == null)
            throw new InvalidOperationException($"{nameof(script.Path)} is not set. Cannot save script.");

        // Basic protection against malicious calls
        if (!script.Path.EndsWithIgnoreCase(Script.STANDARD_EXTENSION))
            throw new InvalidOperationException($"Script file must end with {Script.STANDARD_EXTENSION}");

        await File.WriteAllTextAsync(script.Path, ScriptSerializer.Serialize(script)).ConfigureAwait(false);

        script.IsDirty = false;
        return script;
    }

    public void Rename(Script script, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty", nameof(newName));

        // Basic protection against malicious calls
        if (newName.Contains('/') || newName.Contains('\\'))
            throw new InvalidOperationException($"Script name must not contain a path separator");

        if (script.IsNew)
        {
            script.SetName(newName);
        }
        else
        {
            var oldName = script.Name;
            var oldPath = script.Path;

            script.SetName(newName);
            var newPath = script.Path;

            if (File.Exists(newPath))
            {
                script.SetName(oldName);
                throw new Exception(
                    $"A file already exists at path: {newPath}. Renaming script will overwrite that file");
            }

            File.Move(oldPath!, newPath!, overwrite: false);
        }
    }

    public Task DeleteAsync(Script script)
    {
        if (script.Path == null)
            throw new InvalidOperationException($"{nameof(script.Path)} is not set. Cannot delete script.");

        // Basic protection against malicious calls
        if (!script.Path.EndsWithIgnoreCase(Script.STANDARD_EXTENSION))
            throw new InvalidOperationException($"Script file must end with {Script.STANDARD_EXTENSION}");

        if (!File.Exists(script.Path))
            throw new InvalidOperationException($"{nameof(script.Path)} does not exist. Cannot delete script.");

        File.Delete(script.Path);
        return Task.CompletedTask;
    }

    private IEnumerable<string> EnumerateScriptFiles()
    {
        return Directory.EnumerateFiles(
            GetRepositoryDirPath(),
            $"*.{Script.STANDARD_EXTENSION_WO_DOT}",
            SearchOption.AllDirectories);
    }

    private static Guid? ReadScriptId(string filePath)
    {
        var firstLine = File.ReadLines(filePath).FirstOrDefault();
        return Guid.TryParse(firstLine, out var id) ? id : null;
    }

    private string GetRepositoryDirPath()
    {
        return _settings.ScriptsDirectoryPath;
    }
}
