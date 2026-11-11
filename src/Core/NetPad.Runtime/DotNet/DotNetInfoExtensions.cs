using System.IO;

namespace NetPad.DotNet;

public static class DotNetInfoExtensions
{
    /// <summary>
    /// Gets the latest installed SDK of the target <paramref name="frameworkVersion"/> (e.g. 8.0.416 for DotNet8).
    /// Everything that builds or analyzes a script's project should use this SDK, so that they all see the same
    /// reference assemblies and analyzers.
    /// </summary>
    /// <returns>
    /// The SDK, or <c>null</c> if no SDK of that version is installed or if the latest one was removed after NetPad
    /// listed the installed SDKs (for example, a package manager replaced it with a newer patch).
    /// </returns>
    public static DotNetSdkVersion? LocateLatestSdkForFramework(
        this IDotNetInfo dotNetInfo,
        DotNetFrameworkVersion frameworkVersion)
    {
        var sdk = dotNetInfo.GetDotNetSdkVersions()
            .Where(s => s.IsSupported()
                        && s.GetFrameworkVersion() == frameworkVersion
                        && s.DotNetRootDirectory != null)
            .MaxBy(s => s.Version);

        if (sdk == null || !Directory.Exists(Path.Combine(sdk.DotNetRootDirectory!, "sdk", sdk.Version.ToString())))
        {
            return null;
        }

        return sdk;
    }

    /// <summary>
    /// Gets the version of the newest installed <paramref name="frameworkName"/> runtime of the given major version
    /// (e.g. 9.0.11 for "Microsoft.NETCore.App" and 9). Prerelease runtimes count, so a version is found even when a
    /// prerelease is the only runtime of that major version installed.
    /// </summary>
    /// <param name="dotNetInfo">Information about the installed .NET runtimes.</param>
    /// <param name="frameworkName">
    /// The runtime framework name, e.g. "Microsoft.NETCore.App" or "Microsoft.AspNetCore.App".
    /// </param>
    /// <param name="majorVersion">The major version of the runtime.</param>
    /// <returns>
    /// The runtime version, or <c>null</c> if no runtime of that name and major version is installed.
    /// </returns>
    public static SemanticVersion? LocateLatestRuntimeVersion(
        this IDotNetInfo dotNetInfo,
        string frameworkName,
        int majorVersion)
    {
        return dotNetInfo.GetDotNetRuntimeVersions()
            .Where(r => r.Version.Major == majorVersion &&
                        r.FrameworkName.Equals(frameworkName, StringComparison.OrdinalIgnoreCase))
            .MaxBy(r => r.Version)?
            .Version;
    }
}
