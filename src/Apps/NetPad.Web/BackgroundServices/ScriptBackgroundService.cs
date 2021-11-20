using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.BackgroundServices
{
    public class ScriptBackgroundService : BackgroundService
    {
        private readonly ISession _session;
        private readonly Settings _settings;

        public ScriptBackgroundService(ISession session, Settings settings)
        {
            _session = session;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ReactToScriptPropertyChanged();

            // Electron.IpcMain.RemoveAllListeners("save-script");
            // Electron.IpcMain.On("save-script", async (msg) =>
            // {
            //
            // });
        }

        private void ReactToScriptPropertyChanged()
        {
            _session.OpenScripts.CollectionChanged += (_,  changes) =>
            {
                if (changes.Action == NotifyCollectionChangedAction.Add && changes.NewItems?.Count > 0)
                {
                    foreach (Script script in changes.NewItems)
                    {
                        script.OnPropertyChanged.Add(async (args) =>
                        {
                            Electron.IpcMain.Send(BrowserWindow, "script-property-changed", Serialize(new
                            {
                                ScriptId = script.Id,
                                PropertyName = args.PropertyName,
                                NewValue = args.NewValue
                            }));
                        });
                    }
                }
                else if (changes.Action == NotifyCollectionChangedAction.Remove)
                {
                    var scripts = changes.OldItems as IList<Script>;
                    if (scripts == null) return;

                    foreach (var script in scripts)
                    {
                        script.RemoveAllPropertyChangedHandlers();
                    }
                }
            };
        }
    }
}
