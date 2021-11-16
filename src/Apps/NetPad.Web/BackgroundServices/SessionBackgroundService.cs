using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Sessions;

namespace NetPad.BackgroundServices
{
    public class SessionBackgroundService : BackgroundService
    {
        private readonly ISession _session;

        public SessionBackgroundService(ISession session)
        {
            _session = session;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _session.OpenQueries.CollectionChanged += (_,  changes) =>
            {
                if (changes.Action == NotifyCollectionChangedAction.Add || changes.Action == NotifyCollectionChangedAction.Remove)
                {
                    try
                    {
                        var bw = Electron.WindowManager.BrowserWindows.FirstOrDefault();

                        Electron.IpcMain.Send(
                            bw,
                            "session-query-" + (changes.Action == NotifyCollectionChangedAction.Add ? "added" : "removed"),
                            Serialize(changes.Action == NotifyCollectionChangedAction.Add ? changes.NewItems : changes.OldItems)
                        );
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            };
        }
    }
}
