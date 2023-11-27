using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.Presentation.Html;

namespace NetPad.BackgroundServices;

public class EventHandlerBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;

    public EventHandlerBackgroundService(IEventBus eventBus, ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _eventBus = eventBus;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventBus.Subscribe<SettingsUpdatedEvent>(ev =>
        {
            HtmlPresenter.UpdateSerializerSettings(ev.Settings.Results.MaxSerializationDepth, ev.Settings.Results.MaxCollectionSerializeLength);

            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }
}
