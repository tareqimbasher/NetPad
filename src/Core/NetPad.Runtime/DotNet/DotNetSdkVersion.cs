namespace NetPad.DotNet;

/// <summary>
/// A version of .NET SDK.
/// </summary>
public record DotNetSdkVersion(SemanticVersion Version)
{
    public override string ToString() => Version.ToString();
}
