using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NetPad.Packages;

internal class PackageDependencyTree
{
    public PackageDependencyTree(PackageIdentity packageIdentity)
    {
        Identity = packageIdentity;
        Dependencies = new List<PackageDependencyTree>();
    }

    public PackageIdentity Identity { get; }
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
