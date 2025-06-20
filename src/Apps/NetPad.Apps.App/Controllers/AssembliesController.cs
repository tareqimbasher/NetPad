using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NetPad.Assemblies;
using NetPad.Common;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Packages;

namespace NetPad.Controllers;

[ApiController]
[Route("assemblies")]
public class AssembliesController(IPackageProvider packageProvider) : ControllerBase
{
    [HttpPatch("namespaces")]
    public async Task<HashSet<string>> GetNamespaces([FromBody] Reference reference)
    {
        if (reference is AssemblyFileReference assemblyFileReference)
        {
            if (!System.IO.File.Exists(assemblyFileReference.AssemblyPath))
                throw new Exception($"Assembly path does not exist: {assemblyFileReference.AssemblyPath}");

            using var assemblyInfoReader = new AssemblyInfoReader(assemblyFileReference.AssemblyPath);

            return assemblyInfoReader.GetNamespaces();
        }

        if (reference is PackageReference packageReference)
        {
            var assets = await packageProvider.GetCachedPackageAssetsAsync(
                packageReference.PackageId,
                packageReference.Version,
                GlobalConsts.AppDotNetFrameworkVersion);

            var namespaces = new HashSet<string>();

            foreach (var asset in assets.Where(a => a.IsManagedAssembly))
            {
                using var assemblyInfoReader = new AssemblyInfoReader(asset.Path);

                foreach (var ns in assemblyInfoReader.GetNamespaces())
                {
                    namespaces.Add(ns);
                }
            }

            return namespaces;
        }

        throw new Exception($"Unhandled reference type: {reference.GetType().Name}");
    }
}
