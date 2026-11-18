namespace NetPad.Plugins.OmniSharp.Services;

/// <summary>
/// Thrown when OmniSharp cannot start because the .NET runtime it needs is not installed.
/// </summary>
public class OmniSharpRuntimeMissingException(OmniSharpRuntimeRequirement requirement)
    : Exception($"OmniSharp needs the {requirement}. Install it, then restart NetPad.")
{
    public OmniSharpRuntimeRequirement Requirement { get; } = requirement;
}
