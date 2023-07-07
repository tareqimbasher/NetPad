using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Exceptions;

namespace NetPad.Scripts;

public class FileSystemScriptRepository : IScriptRepository
{
    private readonly Settings _settings;
    private readonly IDataConnectionRepository _dataConnectionRepository;
    private readonly IDotNetInfo _dotNetInfo;

    public FileSystemScriptRepository(Settings settings, IDataConnectionRepository dataConnectionRepository, IDotNetInfo dotNetInfo)
    {
        _settings = settings;
        _dataConnectionRepository = dataConnectionRepository;
        _dotNetInfo = dotNetInfo;
        Directory.CreateDirectory(GetRepositoryDirPath());
    }

    public Task<IEnumerable<ScriptSummary>> GetAllAsync()
    {
        var summaries = new List<ScriptSummary>();

        var scriptFiles = Directory.GetFiles(
            GetRepositoryDirPath(),
            $"*.{Script.STANDARD_EXTENSION_WO_DOT}", SearchOption.AllDirectories);

        foreach (var scriptFile in scriptFiles)
        {
            var firstLine = File.ReadLines(scriptFile).FirstOrDefault();

            if (firstLine == null || !Guid.TryParse(firstLine, out var scriptIdFromFile))
            {
                continue;
            }

            summaries.Add(new ScriptSummary(
                scriptIdFromFile,
                Path.GetFileNameWithoutExtension(scriptFile),
                scriptFile.Replace(Path.PathSeparator, '/')));
        }

        return Task.FromResult<IEnumerable<ScriptSummary>>(summaries);
    }

    public Task<Script> CreateAsync(string name)
    {
        var script = new Script(
            Guid.NewGuid(),
            name,
            new ScriptConfig(ScriptKind.Program, _dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.FrameworkVersion() ?? GlobalConsts.AppDotNetFrameworkVersion));

        return Task.FromResult(script);
    }

    public async Task<Script> GetAsync(string path)
    {
        // Basic protection against malicious calls
        if (!path.EndsWith(Script.STANDARD_EXTENSION, StringComparison.OrdinalIgnoreCase))
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
        var scriptFiles = new DirectoryInfo(GetRepositoryDirPath()).EnumerateFiles(
                $"*.{Script.STANDARD_EXTENSION_WO_DOT}", SearchOption.AllDirectories)
            // Basic protection against malicious calls
            .Where(f => f.Name.EndsWith(Script.STANDARD_EXTENSION, StringComparison.OrdinalIgnoreCase));

        foreach (var scriptFile in scriptFiles)
        {
            var firstLine = File.ReadLines(scriptFile.FullName).FirstOrDefault();
            if (firstLine == null || !Guid.TryParse(firstLine, out var scriptIdFromFile) || scriptId != scriptIdFromFile)
            {
                continue;
            }

            return await GetAsync(scriptFile.FullName);
        }

        return null;
    }

    public async Task<Script> SaveAsync(Script script)
    {
        if (script.Path == null)
            throw new InvalidOperationException($"{nameof(script.Path)} is not set. Cannot save script.");

        // Basic protection against malicious calls
        if (!script.Path.EndsWith(Script.STANDARD_EXTENSION, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Script file must end with {Script.STANDARD_EXTENSION}");

        await File.WriteAllTextAsync(script.Path, ScriptSerializer.Serialize(script)).ConfigureAwait(false);

        script.IsDirty = false;
        return script;
    }

    public Task DeleteAsync(Script script)
    {
        if (script.Path == null)
            throw new InvalidOperationException($"{nameof(script.Path)} is not set. Cannot delete script.");

        // Basic protection against malicious calls
        if (!script.Path.EndsWith(Script.STANDARD_EXTENSION, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Script file must end with {Script.STANDARD_EXTENSION}");

        if (!File.Exists(script.Path))
            throw new InvalidOperationException($"{nameof(script.Path)} does not exist. Cannot delete script.");

        File.Delete(script.Path);
        return Task.CompletedTask;
    }

    private string GetRepositoryDirPath()
    {
        return _settings.ScriptsDirectoryPath;
    }
}
