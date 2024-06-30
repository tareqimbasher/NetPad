using NetPad.Common;
using NetPad.DotNet;

namespace NetPad.Apps.Data.EntityFrameworkCore;

public static class EntityFrameworkPackageUtils
{
    public static Task<string?> GetEntityFrameworkProviderVersionAsync(DotNetFrameworkVersion dotNetFrameworkVersion, string providerName)
    {
        return Task.FromResult<string?>(GlobalConsts.EntityFrameworkLibVersion(dotNetFrameworkVersion, providerName));
    }

    public static Task<string?> GetEntityFrameworkDesignVersionAsync(DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return Task.FromResult<string?>(GlobalConsts.EntityFrameworkLibVersion(dotNetFrameworkVersion, "Microsoft.EntityFrameworkCore.Design"));
    }
}
