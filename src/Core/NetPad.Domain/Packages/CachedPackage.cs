namespace NetPad.Packages
{
    public class CachedPackage
    {
        public CachedPackage(string packageId, string directoryPath, string[] versions)
        {
            PackageId = packageId;
            DirectoryPath = directoryPath;
            Versions = versions;
        }

        public string PackageId { get; }
        public string DirectoryPath { get; }
        public string[] Versions { get; }
        public PackageMetadata? PackageMetadata { get; private set; }

        public CachedPackage SetPackageMetadata(PackageMetadata packageMetadata)
        {
            PackageMetadata = packageMetadata;
            return this;
        }
    }
}
