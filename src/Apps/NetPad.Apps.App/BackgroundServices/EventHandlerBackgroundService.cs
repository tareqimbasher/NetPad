using Microsoft.Extensions.Logging;
using NetPad.Configuration.Events;
using NetPad.Events;
using NetPad.Presentation.Html;

namespace NetPad.BackgroundServices;

public class EventHandlerBackgroundService(IEventBus eventBus, ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<SettingsUpdatedEvent>(ev =>
        {
            HtmlPresenter.UpdateSerializerSettings(ev.Settings.Results.MaxSerializationDepth, ev.Settings.Results.MaxCollectionSerializeLength);

            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }
}
