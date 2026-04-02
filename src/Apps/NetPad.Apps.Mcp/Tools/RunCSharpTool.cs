using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class RunCSharpTool
{
    [McpServerTool(Name = "run_csharp"), Description(
        "Run C# code in NetPad and return the output. " +
        "Code runs in a headless process without GUI interaction. " +
        "The code is executed as a C# program-style script (top-level statements). " +
        "Use .Dump() to output objects and collections (e.g. myList.Dump() or myObj.Dump(\"Title\")). " +
        "When a dataConnectionId is provided, DbSet properties are available as top-level variables " +
        "(e.g. Artists.Take(5).Dump()). Use DataContext to access the DbContext directly. " +
        "SaveChanges() is also available directly for write operations.")]
    public static async Task<string> RunCSharp(
        NetPadApiClient api,
        [Description("The C# code to execute")] string code,
        [Description("Optional NuGet packages as JSON array, e.g. [{\"id\":\"Newtonsoft.Json\",\"version\":\"13.0.3\"}]. Version is required.")]
        string? packages = null,
        [Description("Optional .NET target framework version, e.g. DotNet8, DotNet10")] string? targetFramework = null,
        [Description("Optional data connection ID (GUID) for database access. Use list_data_connections to find available IDs.")] string? dataConnectionId = null,
        [Description("Optional execution timeout in milliseconds")] int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var request = new HeadlessRunRequest { Code = code, Kind = HeadlessRunRequest.KindCSharp, TimeoutMs = timeoutMs };

        if (packages != null)
        {
            request.Packages = JsonSerializer.Deserialize<PackageReferenceDto[]>(packages, JsonDefaults.CaseInsensitiveOptions);
        }

        if (targetFramework != null)
        {
            request.TargetFramework = targetFramework;
        }

        if (dataConnectionId != null)
        {
            if (!Guid.TryParse(dataConnectionId, out var connId))
                return "Invalid dataConnectionId format. Expected a GUID.";
            request.DataConnectionId = connId;
        }

        var result = await api.RunCodeAsync(request, cancellationToken);
        return ExecutionResultFormatter.Format(result);
    }
}
