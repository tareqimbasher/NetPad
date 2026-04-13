using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class UpdateScriptTool
{
    [McpServerTool(Name = "update_script", Destructive = false, Idempotent = true), Description(
         "Update the code and/or configuration of an open script in NetPad. At least one property must be provided.")]
    public static async Task<string> UpdateScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description(
            "New code content for the script. Changes are in-memory until the script is saved. " +
            "Uses C# top-level statements: executable statements must come before any type/method declarations.")]
        string? code = null,
        [Description("Script kind: 'Program' or 'SQL'")]
        string? kind = null,
        [Description("Target .NET framework version, e.g. 'DotNet8', 'DotNet9', 'DotNet10'")]
        string? targetFrameworkVersion = null,
        [Description("Compiler optimization level: 'Debug' or 'Release'")]
        string? optimizationLevel = null,
        [Description("Whether to reference ASP.NET assemblies")]
        bool? useAspNet = null,
        [Description(
            "Namespaces to include. Replaces the entire namespace set. Each entry should " +
            "be a bare namespace name without 'using' prefix or semicolons (e.g. 'System.Numerics').")]
        string[]? namespaces = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        if (code == null && kind == null && targetFrameworkVersion == null
            && optimizationLevel == null && useAspNet == null && namespaces == null)
        {
            return "No properties to update. Provide at least one of: code, kind, targetFrameworkVersion, " +
                   "optimizationLevel, useAspNet, namespaces.";
        }

        // Validate all params before making any API calls
        if (kind != null && !ScriptValidation.ValidKinds.Contains(kind))
        {
            return $"Invalid kind '{kind}'. Valid values: {string.Join(", ", ScriptValidation.ValidKinds)}.";
        }

        if (optimizationLevel != null && !ScriptValidation.ValidOptimizationLevels.Contains(optimizationLevel))
        {
            return $"Invalid optimizationLevel '{optimizationLevel}'. " +
                   $"Valid values: {string.Join(", ", ScriptValidation.ValidOptimizationLevels)}.";
        }

        if (namespaces != null)
        {
            foreach (var ns in namespaces)
            {
                if (ns.StartsWith("using ") || ns.EndsWith(';'))
                {
                    return $"Invalid namespace '{ns}'. Namespaces should not start with 'using ' " +
                           "and must not end with ';'. Use bare namespace names (e.g. 'System.Numerics').";
                }
            }
        }

        // Apply updates
        var updated = new List<string>();

        if (code != null)
        {
            await api.UpdateScriptCodeAsync(id, code, cancellationToken);
            updated.Add("code");
        }

        if (kind != null)
        {
            await api.SetScriptKindAsync(id, kind, cancellationToken);
            updated.Add($"kind ({kind})");
        }

        if (targetFrameworkVersion != null)
        {
            await api.SetScriptTargetFrameworkAsync(id, targetFrameworkVersion, cancellationToken);
            updated.Add($"target framework ({targetFrameworkVersion})");
        }

        if (optimizationLevel != null)
        {
            await api.SetScriptOptimizationLevelAsync(id, optimizationLevel, cancellationToken);
            updated.Add($"optimization level ({optimizationLevel})");
        }

        if (useAspNet != null)
        {
            await api.SetScriptUseAspNetAsync(id, useAspNet.Value, cancellationToken);
            updated.Add($"ASP.NET ({(useAspNet.Value ? "enabled" : "disabled")})");
        }

        if (namespaces != null)
        {
            await api.UpdateScriptNamespacesAsync(id, namespaces, cancellationToken);
            updated.Add("namespaces");
        }

        return $"Script updated: {string.Join(", ", updated)}.";
    }
}
