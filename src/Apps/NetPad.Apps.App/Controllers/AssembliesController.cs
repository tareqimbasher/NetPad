using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NetPad.Assemblies;
using NetPad.Common;
using NetPad.DotNet;
using NetPad.Packages;

namespace NetPad.Controllers;

[ApiController]
[Route("assemblies")]
public class AssembliesController(IPackageProvider packageProvider) : ControllerBase
{
    [HttpPatch("namespaces")]
    public async Task<ActionResult<string[]>> GetNamespaces([FromBody] Reference reference)
    {
        if (reference is AssemblyFileReference assemblyFileReference)
        {
            if (assemblyFileReference.AssemblyPath == null)
                throw new Exception("Assembly path is null.");

            using var assemblyInfoReader = new AssemblyInfoReader(assemblyFileReference.AssemblyPath);

            return Ok(assemblyInfoReader.GetNamespaces());
        }

        if (reference is PackageReference packageReference)
        {
            var assets = await packageProvider.GetCachedPackageAssetsAsync(
                packageReference.PackageId,
                packageReference.Version,
                GlobalConsts.AppDotNetFrameworkVersion);

            var namespaces = new HashSet<string>();

            foreach (var asset in assets.Where(a => a.IsAssembly()))
            {
                using var assemblyInfoReader = new AssemblyInfoReader(asset.Path);

                foreach (var ns in assemblyInfoReader.GetNamespaces())
                {
                    namespaces.Add(ns);
                }
            }

            return Ok(namespaces);
        }

        throw new Exception($"Unhandled reference type: {reference.GetType().Name}");
    }
}
