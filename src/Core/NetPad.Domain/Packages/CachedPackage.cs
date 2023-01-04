namespace NetPad.Packages;

public class CachedPackage : PackageMetadata
{
    public CachedPackage(string packageId, string title) : base(packageId, title)
    {
    }

    public PackageInstallReason InstallReason { get; set; }
    public string? DirectoryPath { get; set; }
}
