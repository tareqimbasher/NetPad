using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotLiquid.Util;
using ElectronNET.API;
using Microsoft.Extensions.Hosting;
using NetPad.Queries;
using NetPad.Sessions;

namespace NetPad.BackgroundServices
{
    public class SessionBackgroundService : BackgroundService
    {
        private readonly ISession _session;
        private readonly JsonSerializerOptions _serializationOptions;

        public SessionBackgroundService(ISession session)
        {
            _session = session;
            _serializationOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _session.OpenQueries.CollectionChanged += (_,  changes) =>
            {
                if (changes.Action == NotifyCollectionChangedAction.Add || changes.Action == NotifyCollectionChangedAction.Remove)
                {
                    Electron.IpcMain.Send(
                        Electron.WindowManager.BrowserWindows.First(),
                        "session-query-" + (changes.Action == NotifyCollectionChangedAction.Add ? "added" : "removed"),
                        JsonSerializer.Serialize(changes.Action == NotifyCollectionChangedAction.Add ? changes.NewItems : changes.OldItems,
                            _serializationOptions)
                    );
                }
            };
        }
    }
}
