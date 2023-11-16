namespace NetPad.UiInterop;

public class IpcMessageBatch
{
    public IpcMessageBatch(IList<IpcMessage> messages)
    {
        Messages = messages;
    }

    public IList<IpcMessage> Messages { get; }
}
