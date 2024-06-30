namespace NetPad.Packages;

public class CachedPackage(string packageId, string title) : PackageMetadata(packageId, title)
{
    public PackageInstallReason InstallReason { get; set; }
    public string? DirectoryPath { get; set; }
}
