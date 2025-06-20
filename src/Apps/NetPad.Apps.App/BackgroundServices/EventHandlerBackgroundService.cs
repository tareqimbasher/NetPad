using Microsoft.Extensions.Logging;
using NetPad.Configuration.Events;
using NetPad.Events;
using NetPad.Presentation.Html;

namespace NetPad.BackgroundServices;

/// <summary>
/// Handles some application level events.
/// </summary>
public class EventHandlerBackgroundService(IEventBus eventBus, ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<SettingsUpdatedEvent>(ev =>
        {
            var resultSettings = ev.Settings.Results;

            HtmlPresenter.UpdateSerializerSettings(
                resultSettings.MaxSerializationDepth,
                resultSettings.MaxCollectionSerializeLength);

            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }
}
