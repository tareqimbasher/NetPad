using NetPad.Application;

namespace NetPad.Events;

public class AppStatusMessagePublished : IEvent
{
    public AppStatusMessagePublished(AppStatusMessage message)
    {
        Message = message;
    }

    public AppStatusMessage Message { get; }
}
