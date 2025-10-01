namespace NetPad.IO.IPC.Stdio;

/// <summary>
/// The JSON-over-STDIO envelope message format used by <see cref="StdioIpcGateway"/>.
/// </summary>
/// <param name="Seq">The unique sequence (order) this message was sent.</param>
/// <param name="Type">The name of the message's runtime type.</param>
/// <param name="Data">The message to send, serialized to a JSON string.</param>
internal sealed record StdioIpcEnvelope(long Seq, string Type, string Data);
