using NetPad.Apps;
using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

/// <summary>
/// Forwards certain EventBus messages that are produced by this plugin to IPC clients.
/// </summary>
public class EventForwardToIpcBackgroundService(
    IEventBus eventBus,
    IIpcService ipcService,
    ILoggerFactory loggerFactory) : BackgroundService(loggerFactory)
{
    private readonly List<IDisposable> _disposables = [];

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        SubscribeAndForwardToIpc<OmniSharpAsyncBufferUpdateCompletedEvent>();

        return Task.CompletedTask;
    }

    private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
    {
        var token = eventBus.Subscribe<TEvent>(async ev => { await ipcService.SendAsync(ev); });
        _disposables.Add(token);
    }

    protected override Task StoppingAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}
