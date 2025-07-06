namespace NetPad.Packages;

/// <summary>
/// Information about the installation of an installed package.
/// </summary>
/// <param name="packageId">The package ID.</param>
/// <param name="version">The package version.</param>
/// <param name="installReason">The reason the package was installed.</param>
public class PackageInstallInfo(string packageId, string version, PackageInstallReason installReason)
{
    public string PackageId { get; } = packageId;
    public string Version { get; } = version;

    /// <summary>
    /// The reason the package was installed.
    /// </summary>
    public PackageInstallReason InstallReason { get; private set; } = installReason;

    public void ChangeInstallReason(PackageInstallReason installReason)
    {
        if (InstallReason != installReason)
        {
            InstallReason = installReason;
        }
    }
}
