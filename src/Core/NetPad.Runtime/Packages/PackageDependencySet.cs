namespace NetPad.Packages;

/// <summary>
/// Represents the collection of package dependencies that are required when installing a parent package
/// for a specific .NET target framework.
/// </summary>
/// <param name="TargetFramework">
/// The target framework moniker (TFM) to which these dependencies apply
/// (for example, "net6.0", "netstandard2.0", etc.).
/// </param>
/// <param name="Packages">
/// An optional array of package identifiers (with any version constraints) that the parent package
/// depends on when targeting the specified framework.
/// If there are no framework‐specific dependencies, this may be null or empty.
/// </param>
public record PackageDependencySet(string TargetFramework, string[]? Packages = null);
