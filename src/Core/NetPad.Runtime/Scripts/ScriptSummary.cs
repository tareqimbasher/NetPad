using NetPad.DotNet;

namespace NetPad.Scripts;

/// <summary>
/// Basic information about a script.
/// </summary>
public record ScriptSummary(
    Guid Id,
    string Name,
    string? Path,
    ScriptKind Kind,
    DotNetFrameworkVersion TargetFrameworkVersion,
    Guid? DataConnectionId);
