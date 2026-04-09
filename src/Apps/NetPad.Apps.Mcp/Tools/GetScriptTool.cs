using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetScriptTool
{
    [McpServerTool(Name = "get_script", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Get a script's details including its code, configuration, data connection, and references. " +
        "Works for both open and saved scripts.")]
    public static async Task<string> GetScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        var script = await api.GetScriptAsync(id, cancellationToken);
        return JsonSerializer.Serialize(script);
    }
}
