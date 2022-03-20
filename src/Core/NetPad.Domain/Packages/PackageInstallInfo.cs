namespace NetPad.Packages;

public class PackageInstallInfo
{
    public PackageInstallReason InstallReason { get; set; }
}

public enum PackageInstallReason
{
    Explicit, Dependency
}
