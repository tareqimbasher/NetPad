namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpDiagnosticsEvent(Guid scriptId, OmniSharpDiagnosticMessage diagnostics)
{
    public Guid ScriptId { get; } = scriptId;
    public OmniSharpDiagnosticMessage Diagnostics { get; } = diagnostics;
}
