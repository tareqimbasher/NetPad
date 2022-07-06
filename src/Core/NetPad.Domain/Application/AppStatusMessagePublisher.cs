using System;
using System.Threading.Tasks;
using NetPad.Events;

namespace NetPad.Application;

public class AppStatusMessagePublisher : IAppStatusMessagePublisher
{
    private readonly IEventBus _eventBus;

    public AppStatusMessagePublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PublishAsync(string text, AppStatusMessagePriority priority = AppStatusMessagePriority.Normal, bool persistant = false)
    {
        await PublishAsync(new AppStatusMessage(text, priority, persistant));
    }

    public async Task PublishAsync(Guid scriptId, string text, AppStatusMessagePriority priority = AppStatusMessagePriority.Normal, bool persistant = false)
    {
        await PublishAsync(new AppStatusMessage(scriptId, text, priority, persistant));
    }

    private async Task PublishAsync(AppStatusMessage message) => await _eventBus.PublishAsync(new AppStatusMessagePublished(message));
}
