namespace NetPad.Packages;

/// <summary>
/// The reason a package was installed.
/// </summary>
public enum PackageInstallReason
{
    /// <summary>
    /// The package was installed due to the user explicitly requesting it.
    /// </summary>
    Explicit,

    /// <summary>
    /// The package was installed as a dependency of another package.
    /// </summary>
    Dependency
}
