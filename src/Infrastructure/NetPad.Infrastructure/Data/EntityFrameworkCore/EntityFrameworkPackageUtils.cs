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
        return BadGlobals.EntityFrameworkProviderLibVersion;
    }

    public static async Task<string?> GetEntityFrameworkDesignVersionAsync(IPackageProvider packageProvider)
    {
        // So it depends on the same EF Core version the app is using
        return BadGlobals.EntityFrameworkLibVersion;
    }
}
