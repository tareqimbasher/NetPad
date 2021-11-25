using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NetPad.Sessions;

namespace NetPad.Scripts
{
    public class FileSystemScriptRepository : IScriptRepository
    {
        private readonly Settings _settings;

        public FileSystemScriptRepository(Settings settings)
        {
            _settings = settings;
        }

        public Task<List<ScriptSummary>> GetAllAsync()
        {
            var summaries = Directory.GetFiles(
                    _settings.ScriptsDirectoryPath,
                    $"*{Script.STANARD_EXTENSION}",
                    SearchOption.AllDirectories)
                .Select(f => new ScriptSummary(
                    Path.GetFileNameWithoutExtension(f),
                    f.Replace(_settings.ScriptsDirectoryPath, String.Empty)))
                .ToList();

            return Task.FromResult(summaries);
        }

        public Task<Script> CreateAsync(string name)
        {
            var script = new Script(Guid.NewGuid(), name);
            script.Config.SetNamespaces(ScriptConfigDefaults.DefaultNamespaces);
            return Task.FromResult(script);
        }

        public async Task<Script> GetAsync(string path)
        {
            var filePath = GetFullPath(path);
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File {path} was not found.");

            var script = new Script(Guid.NewGuid(), Path.GetFileNameWithoutExtension(fileInfo.Name));
            script.SetPath(path);
            await script.LoadAsync(await File.ReadAllTextAsync(filePath).ConfigureAwait(false)).ConfigureAwait(false);

            return script;
        }

        public async Task<Script> SaveAsync(Script script)
        {
            if (script.Path == null)
                throw new InvalidOperationException($"{nameof(script.Path)} is not set. Cannot save script.");

            var filePath = GetFullPath(script.Path);

            var config = JsonSerializer.Serialize(script.Config);

            await File.WriteAllTextAsync(filePath, $"{script.Id}\n" +
                                                   $"{config}\n" +
                                                   $"#Code\n" +
                                                   $"{script.Code}")
                .ConfigureAwait(false);

            script.IsDirty = false;
            return script;
        }

        public Task DeleteAsync(Script script)
        {
            if (script.Path == null)
                throw new InvalidOperationException($"{nameof(script.Path)} is not set. Cannot delete script.");

            var filePath = GetFullPath(script.Path);
            if (!File.Exists(filePath))
                throw new InvalidOperationException($"{nameof(script.Path)} does not exist. Cannot delete script.");

            File.Delete(filePath);
            return Task.CompletedTask;
        }

        private string GetFullPath(string scriptPath) => Path.Combine(_settings.ScriptsDirectoryPath, scriptPath.Trim('/'));
    }
}
