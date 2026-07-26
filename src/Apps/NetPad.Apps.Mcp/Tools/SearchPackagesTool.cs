using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class SearchPackagesTool
{
    [McpServerTool(Name = "search_packages", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Search for NuGet packages by name or keyword. Returns matching packages plus whether more pages " +
        "exist and the names of any package sources that could not be searched.")]
    public static async Task<string> SearchPackages(
        NetPadApiClient api,
        [Description("Search term; empty returns each source's default listing")] string term,
        [Description("Number of packages to skip in each package source for pagination; advance it by 'take'")]
        int skip = 0,
        [Description("Number of packages to take from each package source (max 100)")]
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var results = await api.SearchPackagesAsync(term, skip, take, cancellationToken);
        return JsonSerializer.Serialize(results);
    }
}
