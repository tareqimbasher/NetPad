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
            [FromQuery] int skip = 0,
            [FromQuery] int take = 30,
            [FromQuery] bool includePrerelease = false)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest($"{term} is required.");

            var packages = await _packageProvider.SearchPackagesAsync(
                term,
                skip,
                take,
                includePrerelease
            );

            return Ok(packages);
        }
    }
}
