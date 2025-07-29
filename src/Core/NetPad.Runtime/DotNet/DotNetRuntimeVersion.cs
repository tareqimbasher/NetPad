namespace NetPad.DotNet;

/// <summary>
/// A version of a .NET runtime.
/// </summary>
/// <param name="FrameworkName">
/// The name of the runtime framework. Examples: "Microsoft.NETCore.App", "Microsoft.AspNetCore.App".
/// </param>
/// <param name="Version">The version.</param>
public record DotNetRuntimeVersion(string FrameworkName, SemanticVersion Version)
{
    public override string ToString() => $"{FrameworkName} {Version}";
}
