using NetPad.Application.Events;
using NetPad.Events;

namespace NetPad.Application;

public class AppStatusMessagePublisher(IEventBus eventBus) : IAppStatusMessagePublisher
{
    public async Task PublishAsync(
        string text,
        AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
        bool persistent = false)
    {
        await PublishAsync(new AppStatusMessage(text, priority, persistent));
    }

    public async Task PublishAsync(
        Guid scriptId,
        string text,
        AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
        bool persistent = false)
    {
        await PublishAsync(new AppStatusMessage(scriptId, text, priority, persistent));
    }

    private async Task PublishAsync(AppStatusMessage message) =>
        await eventBus.PublishAsync(new AppStatusMessagePublishedEvent(message));
}
