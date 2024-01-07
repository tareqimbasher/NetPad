using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Assemblies;
using NetPad.Common;
using NetPad.DotNet;
using NetPad.Packages;

namespace NetPad.Controllers;

[ApiController]
[Route("assemblies")]
public class AssembliesController : Controller
{
    private readonly IPackageProvider _packageProvider;

    public AssembliesController(IPackageProvider packageProvider)
    {
        _packageProvider = packageProvider;
    }

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
            var assets = await _packageProvider.GetCachedPackageAssetsAsync(
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
