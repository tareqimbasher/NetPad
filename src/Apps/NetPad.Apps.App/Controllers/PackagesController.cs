using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NetPad.Common;
using NetPad.DotNet;
using NetPad.Packages;

namespace NetPad.Controllers;

[ApiController]
[Route("packages")]
public class PackagesController(IPackageProvider packageProvider) : ControllerBase
{
    [HttpGet("cache")]
    public async Task<ActionResult<IEnumerable<CachedPackage>>> GetCachedPackages([FromQuery] bool loadMetadata)
    {
        return Ok(await packageProvider.GetCachedPackagesAsync(loadMetadata));
    }

    [HttpGet("cache/explicitly-installed")]
    public async Task<ActionResult<IEnumerable<CachedPackage>>> GetExplicitlyInstalledCachedPackages([FromQuery] bool loadMetadata)
    {
        return Ok(await packageProvider.GetExplicitlyInstalledCachedPackagesAsync(loadMetadata));
    }

    [HttpDelete("cache")]
    public async Task<IActionResult> DeleteCachedPackage([FromQuery] string packageId, [FromQuery] string packageVersion)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return BadRequest($"{nameof(packageId)} is required.");
        if (string.IsNullOrWhiteSpace(packageVersion))
            return BadRequest($"{nameof(packageVersion)} is required.");

        await packageProvider.DeleteCachedPackageAsync(packageId, packageVersion);
        return Ok();
    }

    [HttpPatch("cache/purge")]
    public async Task<IActionResult> PurgePackageCache()
    {
        await packageProvider.PurgePackageCacheAsync();
        return Ok();
    }

    [HttpGet("versions")]
    public async Task<string[]> GetPackageVersionsAsync([FromQuery] string packageId, [FromQuery] bool includePrerelease = false)
    {
        return await packageProvider.GetPackageVersionsAsync(packageId, includePrerelease);
    }

    [HttpPost("metadata")]
    public async Task<PackageMetadata[]> GetPackageMetadata([FromBody] PackageIdentity[] packages, CancellationToken cancellationToken)
    {
        return (await packageProvider.GetExtendedMetadataAsync(packages, cancellationToken))
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
        var packages = await packageProvider.SearchPackagesAsync(
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

        var installInfo = await packageProvider.GetPackageInstallInfoAsync(packageId, packageVersion);

        if (installInfo?.InstallReason == PackageInstallReason.Explicit) return Ok();

        await packageProvider.InstallPackageAsync(packageId, packageVersion, dotNetFrameworkVersion ?? GlobalConsts.AppDotNetFrameworkVersion);
        return Ok();
    }
}
