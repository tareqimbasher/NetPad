namespace NetPad.DotNet;

/// <summary>
/// A version of a .NET runtime.
/// </summary>
/// <param name="FrameworkName">
/// The name of the runtime framework. Examples: "Microsoft.NETCore.App", "Microsoft.AspNetCore.App".
/// </param>
/// <param name="Version">The version.</param>
/// <param name="DotNetRootDirectory">The .NET installation root directory this runtime was found in.</param>
public record DotNetRuntimeVersion(string FrameworkName, SemanticVersion Version, string? DotNetRootDirectory = null)
{
    public override string ToString() => $"{FrameworkName} {Version}";
}
