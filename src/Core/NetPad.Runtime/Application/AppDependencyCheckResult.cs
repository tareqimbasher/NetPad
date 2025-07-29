using NetPad.DotNet;

namespace NetPad.Application;

/// <summary>
/// Represents the outcome of verifying that all required .NET dependencies are present
/// and compatible for the application to run correctly.
/// </summary>
/// <param name="DotNetRuntimeVersion">The version of the .NET runtime the application is currently running on.</param>
/// <param name="DotNetSdkVersions">A collection of all .NET SDK versions detected as installed.</param>
/// <param name="DotNetEfToolVersion">
/// The version of the Entity Framework Core command‑line tool (ef), if installed; otherwise, <c>null</c>.
/// </param>
public record AppDependencyCheckResult(
    string DotNetRuntimeVersion,
    SemanticVersion[] DotNetSdkVersions,
    SemanticVersion? DotNetEfToolVersion)
{
    /// <summary>
    /// Gets the subset of <see cref="DotNetSdkVersions"/> that are supported for use in user scripts.
    /// </summary>
    public SemanticVersion[] SupportedDotNetSdkVersionsInstalled =>
        DotNetSdkVersions.Where(DotNetFrameworkVersionUtil.IsSdkVersionSupported).ToArray();

    /// <summary>
    /// Gets a value indicating whether the installed Entity Framework Core command‑line tool (ef) is supported.
    /// </summary>
    public bool IsSupportedDotNetEfToolInstalled =>
        DotNetEfToolVersion != null && DotNetFrameworkVersionUtil.IsEfToolVersionSupported(DotNetEfToolVersion);
}
