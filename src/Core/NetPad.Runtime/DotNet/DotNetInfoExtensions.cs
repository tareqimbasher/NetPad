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
}
