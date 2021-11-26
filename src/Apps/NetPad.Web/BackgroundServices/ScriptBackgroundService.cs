using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Events;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices
{
    public class ScriptBackgroundService : BackgroundService
    {
        private readonly ISession _session;
        private readonly IIpcService _ipcService;

        public ScriptBackgroundService(ISession session, IIpcService ipcService)
        {
            _session = session;
            _ipcService = ipcService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ReactToEnvironmentsChange();

            // Electron.IpcMain.RemoveAllListeners("save-script");
            // Electron.IpcMain.On("save-script", async (msg) =>
            // {
            // });

            return Task.CompletedTask;
        }

        private void ReactToEnvironmentsChange()
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

                        environment.SetIO(ActionRuntimeInputReader.Default, new IpcScriptOutputWriter(environment, _ipcService));
                    }
                }
                else if (changes.Action == NotifyCollectionChangedAction.Remove)
                {
                    var environments = changes.OldItems as IList<ScriptEnvironment>;
                    if (environments == null) return;

                    foreach (var environment in environments)
                    {
                        environment.RemoveAllPropertyChangedHandlers();
                        environment.Script.RemoveAllPropertyChangedHandlers();
                        environment.Script.Config.RemoveAllPropertyChangedHandlers();
                    }
                }
            };
        }
    }
}
