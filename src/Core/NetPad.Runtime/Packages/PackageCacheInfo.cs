namespace NetPad.Packages;

/// <summary>
/// What the package cache currently holds.
/// </summary>
/// <param name="SizeInBytes">The total size on disk of all cached files.</param>
/// <param name="PackageCount">The number of packages in the cache.</param>
public record PackageCacheInfo(long SizeInBytes, int PackageCount);
