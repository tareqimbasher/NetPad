namespace NetPad.Application;

public record AppDependencyCheckResult(string DotNetRuntimeVersion, string? DotNetSdkVersion, string? DotNetEfToolVersion);
