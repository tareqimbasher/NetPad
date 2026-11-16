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
    /// <summary>
    /// The <see cref="FrameworkName"/> of the base .NET runtime.
    /// </summary>
    public const string NetCoreFrameworkName = "Microsoft.NETCore.App";

    /// <summary>
    /// The <see cref="FrameworkName"/> of the ASP.NET Core runtime.
    /// </summary>
    public const string AspNetCoreFrameworkName = "Microsoft.AspNetCore.App";

    public override string ToString() => $"{FrameworkName} {Version}";
}
