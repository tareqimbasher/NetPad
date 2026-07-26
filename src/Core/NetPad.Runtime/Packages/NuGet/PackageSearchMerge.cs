using NuGet.Versioning;

namespace NetPad.Packages.NuGet;

/// <summary>
/// Merges the search results of several package sources into one listing.
/// </summary>
internal static class PackageSearchMerge
{
    /// <summary>
    /// Collapses per-source results into one entry per package ID. When more than one source serves the
    /// same package, the entry that knows about the newer version wins. Entries that are equally new keep
    /// source order.
    /// </summary>
    /// <param name="perSource">Each source's results, in the order the sources are configured.</param>
    public static List<PackageMetadata> ByPackageId(IEnumerable<IEnumerable<PackageMetadata>> perSource)
    {
        var merged = new List<PackageMetadata>();
        var indexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in perSource)
        {
            foreach (var package in source)
            {
                if (indexById.TryGetValue(package.PackageId, out var existing))
                {
                    if (CompareLatestVersion(package, merged[existing]) > 0)
                    {
                        merged[existing] = package;
                    }

                    continue;
                }

                indexById.Add(package.PackageId, merged.Count);
                merged.Add(package);
            }
        }

        return merged;
    }

    /// <summary>
    /// Compares two entries for the same package by the newest version. An entry  with no usable
    /// version loses to one that has it.
    /// </summary>
    public static int CompareLatestVersion(PackageMetadata a, PackageMetadata b)
    {
        var versionA = ParseOrNull(a.LatestAvailableVersion ?? a.Version);
        var versionB = ParseOrNull(b.LatestAvailableVersion ?? b.Version);

        if (versionA == null) return versionB == null ? 0 : -1;
        if (versionB == null) return 1;

        return VersionComparer.Default.Compare(versionA, versionB);

        static NuGetVersion? ParseOrNull(string? version)
            => version != null && NuGetVersion.TryParse(version, out var parsed) ? parsed : null;
    }
}
