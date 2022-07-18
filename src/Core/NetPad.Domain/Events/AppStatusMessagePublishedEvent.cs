using NetPad.Application;

namespace NetPad.Events;

public class AppStatusMessagePublishedEvent : IEvent
{
    public AppStatusMessagePublishedEvent(AppStatusMessage message)
    {
        Message = message;
    }

    public AppStatusMessage Message { get; }
}
