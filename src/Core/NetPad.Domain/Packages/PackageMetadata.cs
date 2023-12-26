using System;
using System.Linq;

namespace NetPad.Packages;

public class PackageMetadata
{
    public PackageMetadata(string packageId, string title)
    {
        PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Dependencies = Array.Empty<PackageDependencySet>();
    }

    public string PackageId { get; set; }
    public string? Version { get; set; }
    public string Title { get; set; }
    public string? Authors { get; set; }
    public string? Description { get; set; }
    public Uri? IconUrl { get; set; }
    public Uri? ProjectUrl { get; set; }
    public Uri? PackageDetailsUrl { get; set; }
    public Uri? LicenseUrl { get; set; }
    public Uri? ReadmeUrl { get; set; }
    public Uri? ReportAbuseUrl { get; set; }
    public bool? RequireLicenseAcceptance { get; set; }
    public PackageDependencySet[] Dependencies { get; set; }
    public long? DownloadCount { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? LatestAvailableVersion { get; set; }

    public bool IsSomeMetadataMetadataMissing()
    {
        return string.IsNullOrWhiteSpace(Version)
               || string.IsNullOrWhiteSpace(Title)
               || string.IsNullOrWhiteSpace(Authors)
               || string.IsNullOrWhiteSpace(Description)
               || IconUrl == null || string.IsNullOrWhiteSpace(IconUrl.ToString())
               || ProjectUrl == null || string.IsNullOrWhiteSpace(ProjectUrl.ToString())
               || PackageDetailsUrl == null || string.IsNullOrWhiteSpace(PackageDetailsUrl.ToString())
               || LicenseUrl == null || string.IsNullOrWhiteSpace(LicenseUrl.ToString())
               || ReadmeUrl == null || string.IsNullOrWhiteSpace(ReadmeUrl.ToString())
               || ReportAbuseUrl == null || string.IsNullOrWhiteSpace(ReportAbuseUrl.ToString())
               || RequireLicenseAcceptance == null
               || !Dependencies.Any()
               || DownloadCount == null
               || PublishedDate == null;
    }
}
