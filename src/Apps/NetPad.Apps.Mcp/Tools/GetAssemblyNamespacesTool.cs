using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetAssemblyNamespacesTool
{
    [McpServerTool(Name = "get_assembly_namespaces", ReadOnly = true, Destructive = false, Idempotent = true),
     Description(
         "Get the namespaces exposed by a NuGet package or an assembly file on disk. " +
         "Useful for discovering what 'using' statements to add after adding a reference to a script. " +
         "Provide either packageId + version for a NuGet package, or assemblyPath for a DLL on disk.")]
    public static async Task<string> GetAssemblyNamespaces(
        NetPadApiClient api,
        [Description("NuGet package ID (e.g. 'Newtonsoft.Json'). Required if assemblyPath is not provided.")]
        string? packageId = null,
        [Description("NuGet package version (e.g. '13.0.3'). Required if packageId is provided.")]
        string? version = null,
        [Description(
            "Path to an assembly file on disk (e.g. '/path/to/MyLib.dll'). Required if packageId is not provided.")]
        string? assemblyPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(packageId))
        {
            if (string.IsNullOrWhiteSpace(version))
                return "version is required when packageId is provided.";

            var namespaces = await api.GetAssemblyNamespacesFromPackageAsync(packageId, version, cancellationToken);
            return JsonSerializer.Serialize(namespaces);
        }

        if (!string.IsNullOrWhiteSpace(assemblyPath))
        {
            var namespaces = await api.GetAssemblyNamespacesFromFileAsync(assemblyPath, cancellationToken);
            return JsonSerializer.Serialize(namespaces);
        }

        return "Either packageId (with version) or assemblyPath must be provided.";
    }
}
