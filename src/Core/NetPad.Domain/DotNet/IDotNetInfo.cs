using System;

namespace NetPad.DotNet;

public interface IDotNetInfo
{
    Version GetCurrentDotNetRuntimeVersion();
    string LocateDotNetRootDirectoryOrThrow();
    string? LocateDotNetRootDirectory();
    string LocateDotNetExecutableOrThrow();
    string? LocateDotNetExecutable();
    DotNetRuntimeVersion[] GetDotNetRuntimeVersionsOrThrow();
    DotNetRuntimeVersion[] GetDotNetRuntimeVersions();
    DotNetSdkVersion[] GetDotNetSdkVersionsOrThrow();
    DotNetSdkVersion[] GetDotNetSdkVersions();
    string LocateDotNetEfToolExecutableOrThrow();
    string? LocateDotNetEfToolExecutable();
    Version? GetDotNetEfToolVersion(string dotNetEfToolExePath);
}
