using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Packages;

namespace NetPad.Data;

public static class EntityFrameworkPackageUtil
{
    private static readonly ConcurrentDictionary<string, string?> _versionCache = new();

    public static string GetEntityFrameworkCoreVersion()
    {
        return BadGlobals.EntityFrameworkLibVersion;
    }

    public static async Task<string?> GetEntityFrameworkProviderVersionAsync(IPackageProvider packageProvider, string providerName)
    {
        if (_versionCache.TryGetValue(providerName, out var version))
            return version;

        var versions = await packageProvider.GetPackageVersionsAsync(providerName);

        var latestVersion = versions
            .Select(v => Version.TryParse(v, out var parsed) ? parsed : null)
            .Where(v => v?.Major == BadGlobals.DotNetVersion)
            .MaxBy(v => v!.ToString())?
            .ToString();

        _versionCache.TryAdd(providerName, latestVersion);
        return latestVersion;
    }

    public static async Task<string?> GetEntityFrameworkDesignVersionAsync(IPackageProvider packageProvider)
    {
        const string packageName = "Microsoft.EntityFrameworkCore.Design";

        if (_versionCache.TryGetValue(packageName, out var version))
            return version;

        var versions = await packageProvider.GetPackageVersionsAsync(packageName);

        var latestVersion = versions
            .Select(v => Version.TryParse(v, out var parsed) ? parsed : null)
            .Where(v => v?.Major == BadGlobals.DotNetVersion)
            .MaxBy(v => v!.ToString())?
            .ToString();

        _versionCache.TryAdd(packageName, latestVersion);
        return latestVersion;
    }
}
