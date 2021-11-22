using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Runtimes;
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
            ReactToEnvironmentsChange();

            // Electron.IpcMain.RemoveAllListeners("save-script");
            // Electron.IpcMain.On("save-script", async (msg) =>
            // {
            //
            // });
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
                            Electron.IpcMain.Send(BrowserWindow, "environment-property-changed", Serialize(new
                            {
                                ScriptId = script.Id,
                                PropertyName = args.PropertyName,
                                NewValue = args.NewValue
                            }));
                            return Task.CompletedTask;
                        });

                        script.OnPropertyChanged.Add((args) =>
                        {
                            Electron.IpcMain.Send(BrowserWindow, "script-property-changed", Serialize(new
                            {
                                ScriptId = script.Id,
                                PropertyName = args.PropertyName,
                                NewValue = args.NewValue
                            }));
                            return Task.CompletedTask;
                        });

                        environment.SetIO(null, new IpcScriptOutputWriter(environment));
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

    public class IpcScriptOutputWriter : IScriptRuntimeOutputWriter
    {
        public ScriptEnvironment Environment { get; }

        public IpcScriptOutputWriter(ScriptEnvironment environment)
        {
            Environment = environment;
        }

        public Task WriteAsync(object? output)
        {
            var data = new
            {
                ScriptId = Environment.Script.Id,
                Output = output?.ToString()
            };

            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(),
                "script-results",
                JsonSerializer.Serialize(data, options: JsonSerializerConfig.DefaultJsonSerializerOptions));

            return Task.CompletedTask;
        }
    }
}
