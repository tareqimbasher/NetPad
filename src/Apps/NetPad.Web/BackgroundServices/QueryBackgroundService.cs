using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Queries;
using NetPad.Sessions;

namespace NetPad.BackgroundServices
{
    public class QueryBackgroundService : BackgroundService
    {
        private readonly ISession _session;
        private readonly Settings _settings;

        public QueryBackgroundService(ISession session, Settings settings)
        {
            _session = session;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ReactToQueryPropertyChanged();

            // Electron.IpcMain.RemoveAllListeners("save-query");
            // Electron.IpcMain.On("save-query", async (msg) =>
            // {
            //
            // });
        }

        private void ReactToQueryPropertyChanged()
        {
            _session.OpenQueries.CollectionChanged += (_,  changes) =>
            {
                if (changes.Action == NotifyCollectionChangedAction.Add && changes.NewItems?.Count > 0)
                {
                    foreach (Query query in changes.NewItems)
                    {
                        query.OnPropertyChanged.Add(async (args) =>
                        {
                            Electron.IpcMain.Send(BrowserWindow, "query-property-changed", Serialize(new
                            {
                                QueryId = query.Id,
                                PropertyName = args.PropertyName,
                                NewValue = args.NewValue
                            }));
                        });
                    }
                }
                else if (changes.Action == NotifyCollectionChangedAction.Remove)
                {
                    var queries = changes.OldItems as IList<Query>;
                    if (queries == null) return;

                    foreach (var query in queries)
                    {
                        query.RemoveAllPropertyChangedHandlers();
                    }
                }
            };
        }
    }
}
