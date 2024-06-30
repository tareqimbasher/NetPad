using System.Text.Json.Serialization;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// A generic IPC message. The <see cref="MessageType"/> property indicates the "channel" the message will be sent over and is equal
/// to the name of the type of the message being sent.
/// </summary>
/// <param name="Message"></param>
/// <param name="CancellationToken"></param>
public record IpcMessage(object Message, [property: JsonIgnore] CancellationToken CancellationToken)
{
    public string MessageType { get; } = Message.GetType().Name;
}
