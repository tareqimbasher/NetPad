namespace NetPad.Packages;

public class PackageInstallInfo
{
    public PackageInstallInfo(string packageId, string version, PackageInstallReason installReason)
    {
        PackageId = packageId;
        Version = version;
        InstallReason = installReason;
    }

    public string PackageId { get; }
    public string Version { get; }
    public PackageInstallReason InstallReason { get; private set; }

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
