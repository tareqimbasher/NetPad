using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NugetPackageIdentity = NuGet.Packaging.Core.PackageIdentity;

namespace NetPad.Packages;

internal class PackageDependencyTree
{
    public PackageDependencyTree(NugetPackageIdentity packageIdentity)
    {
        Identity = packageIdentity;
        Dependencies = new List<PackageDependencyTree>();
    }

    public NugetPackageIdentity Identity { get; }
    public SourcePackageDependencyInfo? DependencyInfo { get; set; }
    public List<PackageDependencyTree> Dependencies { get; }

    public SourcePackageDependencyInfo[] GetAllPackages()
    {
        if (DependencyInfo == null)
            throw new Exception("Dependency info is not loaded.");

        var all = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default) { DependencyInfo };

        all.AddRange(Dependencies.SelectMany(d => d.GetAllPackages()));

        return all.ToArray();
    }
}
