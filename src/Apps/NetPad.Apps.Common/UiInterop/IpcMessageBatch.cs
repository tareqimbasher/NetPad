namespace NetPad.Apps.UiInterop;

public class IpcMessageBatch(IList<IpcMessage> messages)
{
    public IList<IpcMessage> Messages { get; } = messages;
}
