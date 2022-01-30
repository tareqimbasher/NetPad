using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;
using NetPad.Utilities;

namespace NetPad.BackgroundServices
{
    public class ScriptBackgroundService : BackgroundService
    {
        private readonly ISession _session;
        private readonly IIpcService _ipcService;
        private readonly IScriptRepository _scriptRepository;
        private readonly Settings _settings;
        private FileSystemWatcher _scriptDirWatcher;

        public ScriptBackgroundService(
            ISession session,
            IIpcService ipcService,
            IScriptRepository scriptRepository,
            Settings settings)
        {
            _session = session;
            _ipcService = ipcService;
            _scriptRepository = scriptRepository;
            _settings = settings;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PushEnvironmentsChanges();
            PushScriptDirectoryChanges();

            return Task.CompletedTask;
        }

        private void PushEnvironmentsChanges()
        {
            _session.Environments.CollectionChanged += (_,  changes) =>
            {
                if (changes.Action == NotifyCollectionChangedAction.Add && changes.NewItems?.Count > 0)
                {
                    foreach (ScriptEnvironment environment in changes.NewItems)
                    {
                        var script = environment.Script;

                        environment.OnPropertyChanged.Add(async (args) =>
                        {
                            await _ipcService.SendAsync(
                                new EnvironmentPropertyChanged(script.Id, args.PropertyName, args.NewValue));
                        });

                        script.OnPropertyChanged.Add(async (args) =>
                        {
                            await _ipcService.SendAsync(
                                new ScriptPropertyChanged(script.Id, args.PropertyName, args.NewValue));
                        });

                        script.Config.OnPropertyChanged.Add(async (args) =>
                        {
                            await _ipcService.SendAsync(
                                new ScriptConfigPropertyChanged(script.Id, args.PropertyName, args.NewValue));
                        });

                        environment.SetIO(ActionRuntimeInputReader.Null, new IpcScriptOutputWriter(environment, _ipcService));
                    }
                }
                else if (changes.Action == NotifyCollectionChangedAction.Remove)
                {
                    if (changes.OldItems == null) return;

                    foreach (ScriptEnvironment environment in changes.OldItems)
                    {
                        environment.RemoveAllPropertyChangedHandlers();
                        environment.Script.RemoveAllPropertyChangedHandlers();
                        environment.Script.Config.RemoveAllPropertyChangedHandlers();
                    }
                }
            };
        }

        private void PushScriptDirectoryChanges()
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
        }

        private async Task PushDirectoryChanged()
        {
            var scripts = await _scriptRepository.GetAllAsync();
            await _ipcService.SendAsync(new ScriptDirectoryChanged(scripts));
        }
    }
}
