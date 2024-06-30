using System.IO;
using Microsoft.Extensions.Logging;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.BackgroundServices;

/// <summary>
/// Monitors scripts directory, on disk, for changes and notifies IPC clients that it changed.
/// </summary>
public class ScriptsFileWatcherBackgroundService : BackgroundService
{
    private readonly Settings _settings;
    private FileSystemWatcher? _scriptDirWatcher;
    private readonly Action PushDirectoryChanged;

    public ScriptsFileWatcherBackgroundService(IScriptRepository scriptRepository, IIpcService ipcService, Settings settings, ILoggerFactory loggerFactory) :
        base(loggerFactory)
    {
        _settings = settings;

        PushDirectoryChanged = new Func<Task>(async () =>
        {
            var scripts = await scriptRepository.GetAllAsync();
            await ipcService.SendAsync(new ScriptDirectoryChangedEvent(scripts));
        }).DebounceAsync();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitFileWatcher();

        return Task.CompletedTask;
    }

    private void InitFileWatcher()
    {
        _scriptDirWatcher = new FileSystemWatcher(_settings.ScriptsDirectoryPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.FileName
                           | NotifyFilters.DirectoryName
        };

        _scriptDirWatcher.Created += (_, ev) => PushDirectoryChanged();
        _scriptDirWatcher.Deleted += (_, ev) => PushDirectoryChanged();
        _scriptDirWatcher.Renamed += (_, ev) => PushDirectoryChanged();
        _scriptDirWatcher.Error += delegate(object sender, ErrorEventArgs args)
        {
            _logger.LogError(args.GetException(), "Error in FileSystemWatcher. Will re-initialize watcher");
            _scriptDirWatcher.Dispose();
            PushDirectoryChanged();
            InitFileWatcher();
        };

        _scriptDirWatcher.EnableRaisingEvents = true;
    }
}
