namespace NetPad.Application;

public record AppDependencyCheckResult(string DotNetRuntimeVersion, string[] DotNetSdkVersions, string? DotNetEfToolVersion);
