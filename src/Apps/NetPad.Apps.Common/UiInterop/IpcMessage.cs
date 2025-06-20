using System.Text.Json.Serialization;

namespace NetPad.Apps.UiInterop;

// ReSharper disable once NotAccessedPositionalProperty.Global
/// <summary>
/// A message that is sent over an IPC (inter-process communication) connection.
/// </summary>
public record IpcMessage(object Message, [property: JsonIgnore] CancellationToken CancellationToken)
{
    public string MessageType { get; } = Message.GetType().Name;
}
