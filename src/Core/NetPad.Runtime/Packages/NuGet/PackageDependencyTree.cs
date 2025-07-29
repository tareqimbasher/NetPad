using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NugetPackageIdentity = NuGet.Packaging.Core.PackageIdentity;

namespace NetPad.Packages.NuGet;

/// <summary>
/// Represents the dependency tree for a NuGet package.
/// </summary>
internal class PackageDependencyTree(NugetPackageIdentity packageIdentity)
{
    public NugetPackageIdentity Identity { get; } = packageIdentity;
    public SourcePackageDependencyInfo? DependencyInfo { get; set; }
    public List<PackageDependencyTree> Dependencies { get; } = [];

    public SourcePackageDependencyInfo[] GetAllPackages()
    {
        if (DependencyInfo == null)
            throw new Exception("Dependency info is not loaded.");

        var all = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default) { DependencyInfo };

        all.AddRange(Dependencies.SelectMany(d => d.GetAllPackages()));

        return all.ToArray();
    }
}
