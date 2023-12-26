using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Common;
using NetPad.DotNet;
using NetPad.Packages;

namespace NetPad.Controllers;

[ApiController]
[Route("packages")]
public class PackagesController : Controller
{
    private readonly IPackageProvider _packageProvider;

    public PackagesController(IPackageProvider packageProvider)
    {
        _packageProvider = packageProvider;
    }

    [HttpGet("cache")]
    public async Task<ActionResult<IEnumerable<CachedPackage>>> GetCachedPackages([FromQuery] bool loadMetadata)
    {
        return Ok(await _packageProvider.GetCachedPackagesAsync(loadMetadata));
    }

    [HttpGet("cache/explicitly-installed")]
    public async Task<ActionResult<IEnumerable<CachedPackage>>> GetExplicitlyInstalledCachedPackages([FromQuery] bool loadMetadata)
    {
        return Ok(await _packageProvider.GetExplicitlyInstalledCachedPackagesAsync(loadMetadata));
    }

    [HttpDelete("cache")]
    public async Task<IActionResult> DeleteCachedPackage([FromQuery] string packageId, [FromQuery] string packageVersion)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return BadRequest($"{nameof(packageId)} is required.");
        if (string.IsNullOrWhiteSpace(packageVersion))
            return BadRequest($"{nameof(packageVersion)} is required.");

        await _packageProvider.DeleteCachedPackageAsync(packageId, packageVersion);
        return Ok();
    }

    [HttpPatch("cache/purge")]
    public async Task<IActionResult> PurgePackageCache()
    {
        await _packageProvider.PurgePackageCacheAsync();
        return Ok();
    }

    [HttpGet("versions")]
    public async Task<string[]> GetPackageVersionsAsync([FromQuery] string packageId, [FromQuery] bool includePrerelease = false)
    {
        return await _packageProvider.GetPackageVersionsAsync(packageId, includePrerelease);
    }

    [HttpPost("metadata")]
    public async Task<PackageMetadata[]> GetPackageMetadata([FromBody] PackageIdentity[] packages, CancellationToken cancellationToken)
    {
        return (await _packageProvider.GetExtendedMetadataAsync(packages, cancellationToken))
            .Values
            .Where(x => x != null)
            .ToArray()!;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PackageMetadata>>> Search(
        [FromQuery] string? term,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] bool? includePrerelease = null)
    {
        var packages = await _packageProvider.SearchPackagesAsync(
            term,
            skip ?? 0,
            take ?? 30,
            includePrerelease ?? false,
            false
        );

        return Ok(packages);
    }

    [HttpPatch("install")]
    public async Task<IActionResult> Install(
        [FromQuery] string packageId,
        [FromQuery] string packageVersion,
        [FromQuery] DotNetFrameworkVersion? dotNetFrameworkVersion = null)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return BadRequest($"{nameof(packageId)} is required.");
        if (string.IsNullOrWhiteSpace(packageVersion))
            return BadRequest($"{nameof(packageVersion)} is required.");

        var installInfo = await _packageProvider.GetPackageInstallInfoAsync(packageId, packageVersion);

        if (installInfo?.InstallReason == PackageInstallReason.Explicit) return Ok();

        await _packageProvider.InstallPackageAsync(packageId, packageVersion, dotNetFrameworkVersion ?? GlobalConsts.AppDotNetFrameworkVersion);
        return Ok();
    }
}
