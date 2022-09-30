using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Packages;

namespace NetPad.Data.EntityFrameworkCore;

public static class EntityFrameworkPackageUtils
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
        // So it depends on the same EF Core version the app is using
        return BadGlobals.EntityFrameworkLibVersion;
    }
}
