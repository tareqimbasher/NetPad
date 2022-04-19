using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Configuration;
using NetPad.Exceptions;

namespace NetPad.Scripts
{
    public class FileSystemScriptRepository : IScriptRepository
    {
        private readonly Settings _settings;

        public FileSystemScriptRepository(Settings settings)
        {
            _settings = settings;
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
            var script = new Script(Guid.NewGuid(), name);
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

            var script = new Script(Guid.NewGuid(), Path.GetFileNameWithoutExtension(fileInfo.Name));
            script.SetPath(path);
            script.Deserialize(await File.ReadAllTextAsync(path).ConfigureAwait(false));

            return script;
        }

        public async Task<Script?> GetAsync(Guid scriptId)
        {
            var scriptFiles = Directory.EnumerateFiles(
                GetRepositoryDirPath(),
                $"*.{Script.STANDARD_EXTENSION_WO_DOT}", SearchOption.AllDirectories)
                // Basic protection against malicious calls
                .Where(f => f.EndsWith(Script.STANDARD_EXTENSION, StringComparison.OrdinalIgnoreCase));

            foreach (var scriptFile in scriptFiles)
            {
                var firstLine = File.ReadLines(scriptFile).FirstOrDefault();
                if (firstLine == null || !Guid.TryParse(firstLine, out var scriptIdFromFile) || scriptId != scriptIdFromFile)
                {
                    continue;
                }

                var script = Script.From(
                    Path.GetFileNameWithoutExtension(scriptFile),
                    await File.ReadAllTextAsync(scriptFile).ConfigureAwait(false),
                    scriptFile);

                return script;
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

            await File.WriteAllTextAsync(script.Path, script.Serialize()).ConfigureAwait(false);

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
}
