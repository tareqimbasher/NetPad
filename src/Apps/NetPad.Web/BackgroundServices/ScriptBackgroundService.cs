using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Events;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.BackgroundServices
{
    public class ScriptBackgroundService : BackgroundService
    {
        private readonly ISession _session;

        public ScriptBackgroundService(ISession session)
        {
            _session = session;
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

                        environment.OnPropertyChanged.Add((args) =>
                        {
                            Electron.IpcMain.Send(
                                BrowserWindow,
                                nameof(EnvironmentPropertyChanged),
                                Serialize(new EnvironmentPropertyChanged(script.Id, args.PropertyName, args.NewValue)));

                            return Task.CompletedTask;
                        });

                        script.OnPropertyChanged.Add((args) =>
                        {
                            Electron.IpcMain.Send(
                                BrowserWindow,
                                nameof(ScriptPropertyChanged),
                                Serialize(new ScriptPropertyChanged(script.Id, args.PropertyName, args.NewValue)));

                            return Task.CompletedTask;
                        });

                        environment.SetIO(ActionRuntimeInputReader.Default, new IpcScriptOutputWriter(environment));
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
                    }
                }
            };
        }
    }
}
