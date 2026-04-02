using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class SearchPackagesTool
{
    [McpServerTool(Name = "search_packages", ReadOnly = true), Description(
        "Search for NuGet packages by name or keyword.")]
    public static async Task<string> SearchPackages(
        NetPadApiClient api,
        [Description("Search term")] string term,
        [Description("Number of results to skip for pagination")] int skip = 0,
        [Description("Number of results to return (default 30)")] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var packages = await api.SearchPackagesAsync(term, skip, take, cancellationToken);
        return JsonSerializer.Serialize(packages);
    }
}
