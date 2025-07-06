namespace NetPad.Packages;

/// <summary>
/// A package that has already been downloaded and cached locally.
/// </summary>
/// <param name="packageId">The unique ID of the package.</param>
/// <param name="title">The package title.</param>
/// <param name="installReason">The reason this package was downloaded.</param>
/// <param name="directoryPath">The directory that contains this package's assets.</param>
public class CachedPackage(
    string packageId,
    string title,
    PackageInstallReason installReason,
    string directoryPath
) : PackageMetadata(packageId, title)
{
    /// <summary>
    /// The reason this package was downloaded.
    /// </summary>
    public PackageInstallReason InstallReason { get; init; } = installReason;

    /// <summary>
    /// The directory that contains this package's assets.
    /// </summary>
    public string? DirectoryPath { get; init; } = directoryPath;
}
