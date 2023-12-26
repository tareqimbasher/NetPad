namespace NetPad.Packages;

public record PackageDependencySet(string TargetFramework, string[]? Packages = null);
