namespace NetPad.Packages;

/// <summary>
/// Information about a package.
/// </summary>
/// <param name="packageId">The package ID.</param>
/// <param name="title">The package title.</param>
public class PackageMetadata(string packageId, string title)
{
    public string PackageId { get; set; } = packageId ?? throw new ArgumentNullException(nameof(packageId));
    public string? Version { get; set; }
    public string Title { get; set; } = title ?? throw new ArgumentNullException(nameof(title));
    public string? Authors { get; set; }
    public string? Description { get; set; }
    public Uri? IconUrl { get; set; }
    public Uri? ProjectUrl { get; set; }
    public Uri? PackageDetailsUrl { get; set; }
    public Uri? LicenseUrl { get; set; }
    public Uri? ReadmeUrl { get; set; }
    public Uri? ReportAbuseUrl { get; set; }
    public bool? RequireLicenseAcceptance { get; set; }
    public PackageDependencySet[] Dependencies { get; set; } = [];
    public long? DownloadCount { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? LatestAvailableVersion { get; set; }

    public bool HasMissingMetadata()
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
