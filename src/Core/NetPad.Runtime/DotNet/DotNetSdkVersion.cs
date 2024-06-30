namespace NetPad.DotNet;

public record DotNetSdkVersion(SemanticVersion Version)
{
    public override string ToString() => Version.ToString();
}
