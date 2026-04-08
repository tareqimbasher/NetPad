using NetPad.DotNet;

namespace NetPad.Scripts;

/// <summary>
/// Basic information about a script and its current state within the running application.
/// </summary>
public record ScriptInfo(
    Guid Id,
    string Name,
    string? Path,
    ScriptKind Kind,
    DotNetFrameworkVersion TargetFrameworkVersion,
    Guid? DataConnectionId,
    bool IsOpen,
    bool IsDirty,
    ScriptStatus? Status,
    double? RunDurationMilliseconds
) : ScriptSummary(Id, Name, Path, Kind, TargetFrameworkVersion, DataConnectionId);
