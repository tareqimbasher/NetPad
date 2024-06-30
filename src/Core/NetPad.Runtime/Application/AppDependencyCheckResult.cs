using NetPad.DotNet;

namespace NetPad.Application;

public record AppDependencyCheckResult(string DotNetRuntimeVersion, SemanticVersion[] DotNetSdkVersions, SemanticVersion? DotNetEfToolVersion)
{
    public IEnumerable<SemanticVersion> SupportedDotNetSdkVersionsInstalled => DotNetSdkVersions
        .Where(DotNetFrameworkVersionUtil.IsSdkVersionSupported);

    public bool IsSupportedDotNetEfToolInstalled => DotNetEfToolVersion != null && DotNetFrameworkVersionUtil.IsEfToolVersionSupported(DotNetEfToolVersion);
}
