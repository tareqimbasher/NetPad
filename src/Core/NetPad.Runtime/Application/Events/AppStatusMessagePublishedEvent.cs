using NetPad.Events;

namespace NetPad.Application.Events;

public class AppStatusMessagePublishedEvent(AppStatusMessage message) : IEvent
{
    public AppStatusMessage Message { get; } = message;
}
