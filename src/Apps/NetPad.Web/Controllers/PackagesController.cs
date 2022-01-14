using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Packages;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("packages")]
    public class PackagesController : Controller
    {
        private readonly IPackageProvider _packageProvider;

        public PackagesController(IPackageProvider packageProvider)
        {
            _packageProvider = packageProvider;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<PackageMetadata>>> Search(
            [FromQuery] string term,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null,
            [FromQuery] bool? includePrerelease = null)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest($"{term} is required.");

            var packages = await _packageProvider.SearchPackagesAsync(
                term,
                skip ?? 0,
                take ?? 30,
                includePrerelease ?? false
            );

            return Ok(packages);
        }
    }
}
