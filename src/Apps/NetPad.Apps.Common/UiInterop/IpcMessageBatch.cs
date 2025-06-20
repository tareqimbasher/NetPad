namespace NetPad.Apps.UiInterop;

/// <summary>
/// A simple group of <see cref="IpcMessage"/>.
/// </summary>
public class IpcMessageBatch(IList<IpcMessage> messages)
{
    public IList<IpcMessage> Messages { get; } = messages;
}
