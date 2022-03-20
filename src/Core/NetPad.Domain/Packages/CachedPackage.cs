namespace NetPad.Packages
{
    public class CachedPackage : PackageMetadata
    {
        public PackageInstallReason InstallReason { get; set; }
        public string DirectoryPath { get; set; }
    }
}
