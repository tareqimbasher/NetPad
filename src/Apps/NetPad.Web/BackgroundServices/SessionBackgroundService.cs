using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices
{
    public class SessionBackgroundService : BackgroundService
    {
        private readonly ISession _session;
        private readonly IIpcService _ipcService;

        public SessionBackgroundService(ISession session, IIpcService ipcService)
        {
            _session = session;
            _ipcService = ipcService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _session.OnPropertyChanged.Add(async args =>
            {
                if (args.PropertyName == nameof(ISession.Active))
                {
                    await _ipcService.SendAsync(new ActiveEnvironmentChanged(_session.Active?.Script.Id));
                }
            });

            _session.Environments.CollectionChanged += async (_,  changes) =>
            {
                if ((changes.Action == NotifyCollectionChangedAction.Add && changes.NewItems?.Count > 0) ||
                    (changes.Action == NotifyCollectionChangedAction.Remove && changes.OldItems?.Count > 0))
                {
                    if (changes.Action == NotifyCollectionChangedAction.Add)
                    {
                        await _ipcService.SendAsync(
                            new EnvironmentsAdded(changes.NewItems!.Cast<ScriptEnvironment>().ToArray()));
                    }
                    else
                    {
                        await _ipcService.SendAsync(
                            new EnvironmentsRemoved(changes.OldItems!.Cast<ScriptEnvironment>().ToArray()));
                    }
                }
            };

            return Task.CompletedTask;
        }
    }
}
