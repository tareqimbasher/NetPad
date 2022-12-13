using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;
using NetPad.UiInterop;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class EventForwardToIpcBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IIpcService _ipcService;
    private readonly List<IDisposable> _disposables;

    public EventForwardToIpcBackgroundService(IEventBus eventBus, IIpcService ipcService)
    {
        _eventBus = eventBus;
        _ipcService = ipcService;
        _disposables = new List<IDisposable>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SubscribeAndForwardToIpc<OmniSharpAsyncBufferUpdateCompletedEvent>();

        return Task.CompletedTask;
    }

    private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
    {
        var token = _eventBus.Subscribe<TEvent>(async ev => { await _ipcService.SendAsync(ev); });
        _disposables.Add(token);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return base.StopAsync(cancellationToken);
    }
}
