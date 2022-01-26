using System;

namespace NetPad.Packages
{
    public class PackageMetadata
    {
        public string PackageId { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Description { get; set; }
        public Uri IconUrl { get; set; }
        public Uri ProjectUrl { get; set; }
        public long? DownloadCount { get; set; }
        public string[] Versions { get; set; }
    }
}
