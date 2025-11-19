using System.Collections.Concurrent;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Packages;

namespace NetPad.Utilities;

public static class ReferenceUtil
{
    private static readonly ConcurrentDictionary<string, Task<HashSet<PackageAsset>>> _assetCache = new();

    /// <summary>
    /// Gets the file assets that belong to the specified <paramref name="references"/>.
    /// </summary>
    public static async Task<HashSet<ReferenceAsset>> GetAssetsAsync(
        this IEnumerable<Reference> references,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        IPackageProvider packageProvider,
        CancellationToken cancellationToken = default)
    {
        var assets = new HashSet<ReferenceAsset>();

        foreach (var reference in references.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reference is AssemblyImageReference)
            {
                // AssemblyImages don't have file assets
            }
            else if (reference is AssemblyFileReference assemblyFileReference &&
                     assemblyFileReference.AssemblyPath != null)
            {
                assets.Add(new ReferenceAsset(assemblyFileReference.AssemblyPath));
            }
            else if (reference is PackageReference packageReference)
            {
                var cacheKey = $"{packageReference.PackageId}:{packageReference.Version}:{dotNetFrameworkVersion}";
                var cached = _assetCache.GetOrAdd(cacheKey, _ => packageProvider
                    .GetRecursivePackageAssetsAsync(
                        packageReference.PackageId,
                        packageReference.Version,
                        dotNetFrameworkVersion));

                var packageAssets = await cached.ConfigureAwait(false);
                assets.AddRange(packageAssets);
            }
        }

        return assets;
    }

    /// <summary>
    /// Gets the file assets that belong to the specified <paramref name="reference"/>.
    /// </summary>
    public static Task<HashSet<ReferenceAsset>> GetAssetsAsync(
        this Reference reference,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        IPackageProvider packageProvider,
        CancellationToken cancellationToken = default)
    {
        return GetAssetsAsync([reference], dotNetFrameworkVersion, packageProvider, cancellationToken);
    }
}
