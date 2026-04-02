namespace NetPad.Apps.Mcp;

/// <summary>
/// Represents a connection to a running NetPad instance.
/// </summary>
public record NetPadConnection(string Url, string Token, int? Pid = null);
