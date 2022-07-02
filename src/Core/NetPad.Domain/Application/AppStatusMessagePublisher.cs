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
        await _eventBus.PublishAsync(new AppStatusMessagePublished(new AppStatusMessage(text, priority, persistant)));
    }
}
