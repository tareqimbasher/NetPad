using NetPad.DotNet;
using NetPad.Packages;

namespace NetPad.Utilities;

public static class ReferenceUtil
{
    public static async Task<HashSet<ReferenceAsset>> GetAssetsAsync(
        this IEnumerable<Reference> references,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        IPackageProvider packageProvider)
    {
        var assets = new HashSet<ReferenceAsset>();

        foreach (var reference in references.Distinct())
        {
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
                var packageAssets = await packageProvider
                    .GetRecursivePackageAssetsAsync(
                        packageReference.PackageId,
                        packageReference.Version,
                        dotNetFrameworkVersion)
                    .ConfigureAwait(false);

                assets.AddRange(packageAssets);
            }
        }

        return assets;
    }
}
