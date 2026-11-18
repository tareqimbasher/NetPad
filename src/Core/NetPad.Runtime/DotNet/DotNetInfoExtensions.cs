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
    /// Gets the framework versions of the installed SDKs that NetPad supports.
    /// </summary>
    public static DotNetFrameworkVersion[] GetSupportedFrameworkVersions(this IDotNetInfo dotNetInfo)
    {
        return dotNetInfo.GetDotNetSdkVersions()
            .Where(s => s.IsSupported())
            .Select(s => s.GetFrameworkVersion())
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Gets whether an ASP.NET Core runtime is installed for the target <paramref name="frameworkVersion"/>.
    /// </summary>
    public static bool IsAspNetRuntimeInstalledFor(this IDotNetInfo dotNetInfo, DotNetFrameworkVersion frameworkVersion)
    {
        return dotNetInfo.IsRuntimeInstalled(
            DotNetRuntimeVersion.AspNetCoreFrameworkName,
            frameworkVersion.GetMajorVersion(),
            dotNetInfo.LocateDotNetRootDirectoryForFramework(frameworkVersion));
    }

    /// <summary>
    /// Gets whether a runtime of the <paramref name="frameworkName"/> framework is installed in a .NET root, at the
    /// given major version or, if <paramref name="allowNewerMajorVersion"/> is set, at a newer one. Prerelease
    /// runtimes count.
    /// </summary>
    /// <param name="dotNetInfo">Information about the installed .NET runtimes.</param>
    /// <param name="frameworkName">
    /// The runtime framework name, e.g. "Microsoft.NETCore.App" or "Microsoft.AspNetCore.App".
    /// </param>
    /// <param name="majorVersion">The major version of the runtime.</param>
    /// <param name="dotNetRoot">
    /// The .NET root the runtime must be in, or <c>null</c> to accept any root. A runtime whose root is unknown is
    /// accepted either way.
    /// </param>
    /// <param name="allowNewerMajorVersion">Whether a runtime of a newer major version also counts.</param>
    public static bool IsRuntimeInstalled(
        this IDotNetInfo dotNetInfo,
        string frameworkName,
        int majorVersion,
        string? dotNetRoot,
        bool allowNewerMajorVersion = false)
    {
        return dotNetInfo.GetDotNetRuntimeVersions().Any(r =>
            r.FrameworkName.Equals(frameworkName, StringComparison.OrdinalIgnoreCase)
            && (r.Version.Major == majorVersion || (allowNewerMajorVersion && r.Version.Major > majorVersion))
            && (dotNetRoot == null || r.DotNetRootDirectory == null || r.DotNetRootDirectory == dotNetRoot));
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
