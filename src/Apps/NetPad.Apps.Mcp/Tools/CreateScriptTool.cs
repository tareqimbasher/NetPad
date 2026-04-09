using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class CreateScriptTool
{
    [McpServerTool(Name = "create_script", Destructive = false), Description(
         "Create a new script in NetPad. The script will be opened in the editor. " +
         "Defaults: kind=Program, targetFramework=latest installed .NET SDK, optimizationLevel=Debug, useAspNet=false. " +
         "Default namespaces include System, System.IO, System.Linq, System.Collections.Generic, System.Threading.Tasks, and others.")]
    public static async Task<string> CreateScript(
        NetPadApiClient api,
        [Description("Optional name for the script")]
        string? name = null,
        [Description("Optional initial code for the script")]
        string? code = null,
        [Description("Optional data connection ID (GUID) to attach")]
        string? dataConnectionId = null,
        [Description("Script kind: 'Program' (default) or 'SQL'")]
        string? kind = null,
        [Description(
            "Target .NET framework version, e.g. 'DotNet8', 'DotNet9', 'DotNet10'. Defaults to latest installed.")]
        string? targetFrameworkVersion = null,
        [Description("Compiler optimization level: 'Debug' (default) or 'Release'")]
        string? optimizationLevel = null,
        [Description("Whether to reference ASP.NET assemblies. Defaults to false.")]
        bool? useAspNet = null,
        [Description(
            "Namespaces to include. If provided, replaces the default namespace set entirely. Each entry should " +
            "be a bare namespace name without 'using' prefix or semicolons (e.g. 'System.Numerics').")]
        string[]? namespaces = null,
        [Description("Whether to run the script immediately after creation and return output")]
        bool runImmediately = false,
        CancellationToken cancellationToken = default)
    {
        if (kind != null && !ScriptValidation.ValidKinds.Contains(kind))
        {
            return $"Invalid kind '{kind}'. Valid values: {string.Join(", ", ScriptValidation.ValidKinds)}.";
        }

        if (optimizationLevel != null && !ScriptValidation.ValidOptimizationLevels.Contains(optimizationLevel))
        {
            return
                $"Invalid optimizationLevel '{optimizationLevel}'. Valid values: {string.Join(", ", ScriptValidation.ValidOptimizationLevels)}.";
        }

        Guid? connId = null;
        if (dataConnectionId != null)
        {
            if (!Guid.TryParse(dataConnectionId, out var parsed))
                return "Invalid dataConnectionId format. Expected a GUID.";
            connId = parsed;
        }

        var dto = new CreateScriptDto
        {
            Name = name,
            Code = code,
            DataConnectionId = connId,
            Kind = kind,
            TargetFrameworkVersion = targetFrameworkVersion,
            OptimizationLevel = optimizationLevel,
            UseAspNet = useAspNet,
            Namespaces = namespaces
        };

        var script = await api.CreateScriptAsync(dto, cancellationToken);

        if (runImmediately)
        {
            var runResult = await api.RunScriptInGuiAsync(script.Id, cancellationToken: cancellationToken);

            var result = new
            {
                Script = script,
                RunResult = runResult
            };

            return JsonSerializer.Serialize(result);
        }

        return JsonSerializer.Serialize(script);
    }
}
