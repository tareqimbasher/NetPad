namespace NetPad.DotNet;

/// <summary>
/// A version of .NET SDK.
/// </summary>
public record DotNetSdkVersion(SemanticVersion Version, string? DotNetRootDirectory = null)
{
    public override string ToString() => Version.ToString();
}
