using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NetPad.Sessions;

namespace NetPad.Scripts
{
    public class ScriptRepository : IScriptRepository
    {
        private readonly Settings _settings;
        private readonly ISession _session;

        public ScriptRepository(Settings settings, ISession session)
        {
            _settings = settings;
            _session = session;
        }

        public Task<List<ScriptSummary>> GetAllAsync()
        {
            var summaries = Directory.GetFiles(_settings.ScriptsDirectoryPath, "*.netpad", SearchOption.AllDirectories)
                .Select(f => new ScriptSummary(Path.GetFileNameWithoutExtension(f), f.Replace(_settings.ScriptsDirectoryPath, String.Empty)))
                .ToList();

            return Task.FromResult(summaries);
        }

        public Task<Script> CreateAsync()
        {
            var script = new Script(Guid.NewGuid(), GetNewScriptName());

            _session.Add(script);

            return Task.FromResult(script);
        }

        public async Task<Script> OpenAsync(string path)
        {
            var filePath = GetFullPath(path);
            var script = _session.Get(filePath);
            if (script != null)
                return script;

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File {path} was not found.");

            script = new Script(Guid.NewGuid(), fileInfo.Name);
            script.SetPath(path);
            await script.LoadAsync(await File.ReadAllTextAsync(filePath).ConfigureAwait(false)).ConfigureAwait(false);

            _session.Add(script);

            return script;
        }

        public Task<Script> DuplicateAsync(Script script, ScriptDuplicationOptions options)
        {
            throw new System.NotImplementedException();
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

        public Task<Script> DeleteAsync(Script script)
        {
            throw new System.NotImplementedException();
        }

        public Task CloseAsync(Guid id)
        {
            _session.Remove(id);
            return Task.CompletedTask;
        }

        private string GetNewScriptName()
        {
            const string baseName = "Script";
            int number = 1;

            while (_session.OpenScripts.Any(q => q.Name == $"{baseName} {number}"))
            {
                number++;
            }

            return $"{baseName} {number}";
        }

        private string GetFullPath(string scriptPath) => Path.Combine(_settings.ScriptsDirectoryPath, scriptPath.Trim('/'));
    }
}
