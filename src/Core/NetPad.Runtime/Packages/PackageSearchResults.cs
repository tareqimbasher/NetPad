namespace NetPad.Packages;

/// <summary>
/// The result of a package search across all configured package sources.
/// </summary>
/// <param name="Packages">The matching packages, at most one entry per package ID.</param>
/// <param name="HasMorePages"> Whether at least one source has more results past the requested page.</param>
/// <param name="UnavailableSources">The names of the sources that could not be searched.</param>
public record PackageSearchResults(
    PackageMetadata[] Packages,
    bool HasMorePages,
    string[] UnavailableSources);
