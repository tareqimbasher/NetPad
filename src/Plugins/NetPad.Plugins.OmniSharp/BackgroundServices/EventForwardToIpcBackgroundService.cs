using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class EventForwardToIpcBackgroundService(IEventBus eventBus, IIpcService ipcService) : BackgroundService
{
    private readonly List<IDisposable> _disposables = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SubscribeAndForwardToIpc<OmniSharpAsyncBufferUpdateCompletedEvent>();

        return Task.CompletedTask;
    }

    private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
    {
        var token = eventBus.Subscribe<TEvent>(async ev => { await ipcService.SendAsync(ev); });
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
