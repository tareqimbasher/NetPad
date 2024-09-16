namespace NetPad.DotNet;

public interface IDotNetInfo
{
    SemanticVersion GetCurrentDotNetRuntimeVersion();
    string LocateDotNetRootDirectoryOrThrow();
    string? LocateDotNetRootDirectory();
    string LocateDotNetExecutableOrThrow();
    string? LocateDotNetExecutable();
    DotNetRuntimeVersion[] GetDotNetRuntimeVersionsOrThrow();
    DotNetRuntimeVersion[] GetDotNetRuntimeVersions();
    DotNetSdkVersion[] GetDotNetSdkVersionsOrThrow();
    DotNetSdkVersion[] GetDotNetSdkVersions();
    DotNetSdkVersion GetLatestSupportedDotNetSdkVersionOrThrow(bool includePrerelease = false);
    DotNetSdkVersion? GetLatestSupportedDotNetSdkVersion(bool includePrerelease = false);
    string LocateDotNetEfToolExecutableOrThrow();
    string? LocateDotNetEfToolExecutable();
    SemanticVersion? GetDotNetEfToolVersion(string dotNetEfToolExePath);
}
