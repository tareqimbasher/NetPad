using System.Threading;
using System.Threading.Tasks;
using NetPad.Events;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices
{
    public class SessionBackgroundService : BackgroundService
    {
        private readonly IEventBus _eventBus;
        private readonly IIpcService _ipcService;

        public SessionBackgroundService(IEventBus eventBus, IIpcService ipcService)
        {
            _eventBus = eventBus;
            _ipcService = ipcService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SubscribeAndForwardToIpc<ActiveEnvironmentChanged>();
            SubscribeAndForwardToIpc<EnvironmentsAdded>();
            SubscribeAndForwardToIpc<EnvironmentsRemoved>();

            return Task.CompletedTask;
        }

        private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
        {
            _eventBus.Subscribe<TEvent>(async ev =>
            {
                await _ipcService.SendAsync(ev);
            });
        }
    }
}
