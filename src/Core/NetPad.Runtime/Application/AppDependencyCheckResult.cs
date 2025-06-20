using NetPad.DotNet;

namespace NetPad.Application;

/// <summary>
/// The result of the checking for dependencies needed for the app to function properly.
/// </summary>
/// <param name="DotNetRuntimeVersion"></param>
/// <param name="DotNetSdkVersions"></param>
/// <param name="DotNetEfToolVersion"></param>
public record AppDependencyCheckResult(
    string DotNetRuntimeVersion,
    SemanticVersion[] DotNetSdkVersions,
    SemanticVersion? DotNetEfToolVersion)
{
    public SemanticVersion[] SupportedDotNetSdkVersionsInstalled =>
        DotNetSdkVersions.Where(DotNetFrameworkVersionUtil.IsSdkVersionSupported).ToArray();

    public bool IsSupportedDotNetEfToolInstalled =>
        DotNetEfToolVersion != null && DotNetFrameworkVersionUtil.IsEfToolVersionSupported(DotNetEfToolVersion);
}
