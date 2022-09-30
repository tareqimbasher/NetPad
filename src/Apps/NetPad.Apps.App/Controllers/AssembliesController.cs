using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Assemblies;
using NetPad.DotNet;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("assemblies")]
    public class AssembliesController : Controller
    {
        private readonly IAssemblyInfoReader _assemblyInfoReader;
        private readonly IPackageProvider _packageProvider;

        public AssembliesController(IAssemblyInfoReader assemblyInfoReader, IPackageProvider packageProvider)
        {
            _assemblyInfoReader = assemblyInfoReader;
            _packageProvider = packageProvider;
        }

        [HttpPatch("namespaces")]
        public async Task<ActionResult<string[]>> GetNamespaces([FromBody] Reference reference)
        {
            if (reference is AssemblyFileReference assemblyFileReference)
            {
                if (assemblyFileReference.AssemblyPath == null)
                    throw new Exception("Assembly path is null.");

                return Ok(_assemblyInfoReader.GetNamespaces(await System.IO.File.ReadAllBytesAsync(assemblyFileReference.AssemblyPath)));
            }
            else if (reference is PackageReference packageReference)
            {
                var assemblies = await _packageProvider.GetCachedPackageAssembliesAsync(
                    packageReference.PackageId,
                    packageReference.Version);

                var namespaces = new HashSet<string>();

                foreach (var assembly in assemblies)
                {
                    foreach (var ns in _assemblyInfoReader.GetNamespaces(await System.IO.File.ReadAllBytesAsync(assembly)))
                    {
                        namespaces.Add(ns);
                    }
                }

                return Ok(namespaces);
            }
            else
            {
                throw new Exception($"Unhandled reference type: {reference.GetType().Name}");
            }
        }
    }
}
