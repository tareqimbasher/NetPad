using System.ComponentModel;
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
        "Top-level executable statements must come before any type/method declarations. " +
        "Supports NuGet package references (versions auto-resolve if omitted) and assembly file references. " +
        "Default namespaces are pre-imported (System, System.Linq, System.Collections.Generic, System.IO, " +
        "System.Text, System.Threading.Tasks, etc.) so you don't need explicit 'using' statements for common types. " +
        "Use .Dump() to output objects and collections (e.g. myList.Dump() or myObj.Dump(\"Title\")). " +
        "Console.WriteLine also works for simple text output. " +
        "When a dataConnectionId is provided, DbSet properties are available as top-level variables " +
        "(e.g. Artists.Take(5).Dump()). Use DataContext to access the DbContext directly. " +
        "SaveChanges() is also available directly for write operations.")]
    public static async Task<string> RunCSharp(
        NetPadApiClient api,
        [Description("The C# code to execute")] string code,
        [Description("Optional NuGet packages to include. Version is optional — latest stable is used if omitted.")]
        PackageInput[]? packages = null,
        [Description("Optional assembly file paths to reference (e.g. ['/path/to/MyLib.dll'])")]
        string[]? assemblyPaths = null,
        [Description("Optional .NET target framework version, e.g. DotNet8, DotNet10")] string? targetFramework = null,
        [Description("Optional data connection ID (GUID) for database access. Use list_data_connections to find available IDs.")] string? dataConnectionId = null,
        [Description("Optional execution timeout in milliseconds")] int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var request = new HeadlessRunRequest { Code = code, Kind = HeadlessRunRequest.KindCSharp, TimeoutMs = timeoutMs };

        if (packages is { Length: > 0 })
        {
            var (resolved, error) = await PackageReferenceHelper.ResolveVersionsAsync(api, packages, cancellationToken);
            if (error != null) return error;
            packages = resolved;
        }

        if (packages is { Length: > 0 } || assemblyPaths is { Length: > 0 })
        {
            request.References = PackageReferenceHelper.BuildReferenceDtos(packages, assemblyPaths).ToArray();
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
