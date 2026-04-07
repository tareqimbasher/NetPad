using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class CreateScriptTool
{
    [McpServerTool(Name = "create_script"), Description(
        "Create a new script in NetPad. The script will be opened in the editor.")]
    public static async Task<string> CreateScript(
        NetPadApiClient api,
        [Description("Optional name for the script")] string? name = null,
        [Description("Optional initial code for the script")] string? code = null,
        [Description("Optional data connection ID (GUID) to attach")] string? dataConnectionId = null,
        [Description("Whether to run the script immediately after creation and return output")] bool runImmediately = false,
        CancellationToken cancellationToken = default)
    {
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
            DataConnectionId = connId
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
