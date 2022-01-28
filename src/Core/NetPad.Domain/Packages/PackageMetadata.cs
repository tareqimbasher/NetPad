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
        public Uri PackageDetailsUrl { get; set; }
        public Uri LicenseUrl { get; set; }
        public Uri ReadmeUrl { get; set; }
        public Uri ReportAbuseUrl { get; set; }
        public string[] Dependencies { get; set; }
        public long? DownloadCount { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? Version { get; set; }
    }
}
