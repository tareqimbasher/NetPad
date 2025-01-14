namespace NetPad.ExecutionModel.ClientServer.ScriptHost;

/// <summary>
/// A message that is sent to, or received from, the script-host process.
/// </summary>
/// <param name="Seq">The sequence number of the message. Starts at 1. The sequence is assumed to be reset on each script run.</param>
/// <param name="Type">The fully qualified name (<see cref="System.Type.FullName"/>) of the type of the message being sent.</param>
/// <param name="Data">The data payload of the message.</param>
public sealed record ScriptHostIpcMessage(uint Seq, string Type, string Data);
