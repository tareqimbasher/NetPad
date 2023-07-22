namespace NetPad.DotNet;

public record DotNetRuntimeVersion(string FrameworkName, SemanticVersion Version)
{
    public override string ToString() => $"{FrameworkName} {Version}";
}
