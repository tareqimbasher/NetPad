using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (reference is AssemblyFileReference assemblyFileReference && assemblyFileReference.AssemblyPath != null)
            {
                assets.Add(new ReferenceAsset(assemblyFileReference.AssemblyPath));
            }
            else if (reference is PackageReference packageReference)
            {
                var packageAssets = await packageProvider
                    .GetPackageAndDependencyAssetsAsync(packageReference.PackageId, packageReference.Version, dotNetFrameworkVersion)
                    .ConfigureAwait(false);

                assets.AddRange(packageAssets);
            }
        }

        return assets;
    }
}
