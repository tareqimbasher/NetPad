namespace NetPad.DotNet;

/// <summary>
/// Gets information about locally installed .NET runtimes and SDKs.
/// </summary>
public interface IDotNetInfo
{
    SemanticVersion GetCurrentDotNetRuntimeVersion();
    string LocateDotNetRootDirectoryOrThrow();
    string? LocateDotNetRootDirectory();
    string LocateDotNetExecutableOrThrow();
    string? LocateDotNetExecutable();
    DotNetRuntimeVersion[] GetDotNetRuntimeVersionsOrThrow();

    /// <summary>
    /// Gets the .NET runtimes installed in all the .NET roots found. A runtime installed in more than one root is
    /// listed once per root.
    /// </summary>
    DotNetRuntimeVersion[] GetDotNetRuntimeVersions();
    DotNetSdkVersion[] GetDotNetSdkVersionsOrThrow();
    DotNetSdkVersion[] GetDotNetSdkVersions();
    DotNetSdkVersion GetLatestSupportedDotNetSdkVersionOrThrow(bool includePrerelease = false);
    DotNetSdkVersion? GetLatestSupportedDotNetSdkVersion(bool includePrerelease = false);
    string? LocateDotNetRootDirectoryForFramework(DotNetFrameworkVersion frameworkVersion);
    string? LocateDotNetExecutableForFramework(DotNetFrameworkVersion frameworkVersion);
}
