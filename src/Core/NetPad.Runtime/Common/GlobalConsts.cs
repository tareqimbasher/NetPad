using NetPad.DotNet;

namespace NetPad.Common;

/// <summary>
/// Global constants.
/// </summary>
public static class GlobalConsts
{
    /// <summary>
    /// The .NET Runtime version the app is currently running on.
    /// </summary>
    public static DotNetFrameworkVersion AppDotNetFrameworkVersion { get; } =
        DotNetFrameworkVersionUtil.GetFrameworkVersion(Environment.Version.Major);

    /// <summary>
    /// If a data connection's cached resources were created using an app that had a version before this version, it
    /// will be invalidated and re-scaffolded.
    /// </summary>
    public static SemanticVersion DataConnectionCacheValidOnOrAfterAppVersion { get; } = new(0, 9, 1);
}
