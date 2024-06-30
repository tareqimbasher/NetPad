namespace NetPad.Packages;

public class PackageInstallInfo(string packageId, string version, PackageInstallReason installReason)
{
    public string PackageId { get; } = packageId;
    public string Version { get; } = version;
    public PackageInstallReason InstallReason { get; private set; } = installReason;

    public void ChangeInstallReason(PackageInstallReason installReason)
    {
        if (InstallReason != installReason)
        {
            InstallReason = installReason;
        }
    }
}

public enum PackageInstallReason
{
    Explicit,
    Dependency
}
