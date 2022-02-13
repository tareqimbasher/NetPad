using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices;

public class ScriptDirectoryBackgroundService : BackgroundService
{
    private readonly IScriptRepository _scriptRepository;
    private readonly IIpcService _ipcService;
    private readonly Settings _settings;
    private FileSystemWatcher? _scriptDirWatcher;

    public ScriptDirectoryBackgroundService(IScriptRepository scriptRepository, IIpcService ipcService, Settings settings)
    {
        _scriptRepository = scriptRepository;
        _ipcService = ipcService;
        _settings = settings;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _scriptDirWatcher = new FileSystemWatcher(_settings.ScriptsDirectoryPath)
        {
            Filter = "*.netpad",
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.FileName
                           | NotifyFilters.DirectoryName
        };

        _scriptDirWatcher.Created += async (_, ev) => await PushDirectoryChanged();
        _scriptDirWatcher.Deleted += async (_, ev) => await PushDirectoryChanged();
        _scriptDirWatcher.Renamed += async (_, ev) => await PushDirectoryChanged();

        _scriptDirWatcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    private async Task PushDirectoryChanged()
    {
        var scripts = await _scriptRepository.GetAllAsync();
        await _ipcService.SendAsync(new ScriptDirectoryChanged(scripts));
    }
}
