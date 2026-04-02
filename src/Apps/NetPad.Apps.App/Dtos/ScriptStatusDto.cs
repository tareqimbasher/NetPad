using NetPad.Scripts;

namespace NetPad.Dtos;

public class ScriptStatusDto
{
    public Guid ScriptId { get; init; }
    public required string Name { get; init; }
    public required ScriptStatus Status { get; init; }
    public double? RunDurationMs { get; init; }
}
