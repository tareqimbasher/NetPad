using Microsoft.Extensions.Logging;
using NetPad.Apps.Scripts;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Apps.Data;

public class FileSystemExtensionCodeProvider(Settings settings, IDataConnectionRepository repository, IDotNetInfo info) : IExtensionsCodeProvider
{
    public async Task<IEnumerable<Script>> GetAll(Guid currentScriptId)
    {
        var path = Path.Combine(settings.ScriptsDirectoryPath, "Extensions");
        if (!Directory.Exists(path))
            return [];

        List<Script> scripts = new();
        foreach (var file in Directory.GetFileSystemEntries(path))
        {
            var contents = await File.ReadAllTextAsync(file);
            if (!Path.GetExtension(file).EqualsIgnoreCase(Script.STANDARD_EXTENSION))
                continue;

            var script = await ScriptSerializer.DeserializeAsync(Script.GetNameFromPath(file), contents, repository, info);

            if (script.Id == currentScriptId)
                continue;

            scripts.Add(script);
        }

        return scripts;
    }
}
