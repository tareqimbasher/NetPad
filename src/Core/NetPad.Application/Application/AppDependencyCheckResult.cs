using NetPad.DotNet;

namespace NetPad.Application;

public record AppDependencyCheckResult(string DotNetRuntimeVersion, string[] DotNetSdkVersions, string? DotNetEfToolVersion)
{
    public IEnumerable<string> SupportedDotNetSdkVersionsInstalled => DotNetSdkVersions
        .Where(v => DotNetFrameworkVersionUtil.IsSdkVersionSupported(Version.Parse(v)));

    public bool IsSupportedDotNetEfToolInstalled => !string.IsNullOrWhiteSpace(DotNetEfToolVersion);
}
