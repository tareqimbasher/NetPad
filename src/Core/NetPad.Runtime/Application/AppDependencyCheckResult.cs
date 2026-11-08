using NetPad.DotNet;

namespace NetPad.Application;

/// <summary>
/// Represents the outcome of verifying that all required .NET dependencies are present
/// and compatible for the application to run correctly.
/// </summary>
/// <param name="DotNetRuntimeVersion">The version of the .NET runtime the application is currently running on.</param>
/// <param name="DotNetSdkVersions">A collection of all .NET SDK versions detected as installed.</param>
public record AppDependencyCheckResult(
    string DotNetRuntimeVersion,
    DotNetSdkVersion[] DotNetSdkVersions)
{
    /// <summary>
    /// Gets the subset of <see cref="DotNetSdkVersions"/> that are supported for use in user scripts.
    /// </summary>
    public DotNetSdkVersion[] SupportedDotNetSdkVersionsInstalled =>
        DotNetSdkVersions.Where(v => v.IsSupported()).ToArray();
}
