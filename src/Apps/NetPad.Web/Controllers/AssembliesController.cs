using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Assemblies;
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
            string assemblyPath;

            if (reference is AssemblyReference assemblyReference)
            {
                if (assemblyReference.AssemblyPath == null)
                    throw new Exception("Assembly path is null.");

                assemblyPath = assemblyReference.AssemblyPath;
            }
            else if (reference is PackageReference packageReference)
            {
                assemblyPath = await _packageProvider.GetCachedPackageAssemblyPathAsync(
                    packageReference.PackageId,
                    packageReference.Version);
}
            else
            {
                throw new Exception($"Unknown reference type: {reference.GetType().Name}");
            }

            return Ok(_assemblyInfoReader.GetNamespaces(await System.IO.File.ReadAllBytesAsync(assemblyPath)));
        }
    }
}
