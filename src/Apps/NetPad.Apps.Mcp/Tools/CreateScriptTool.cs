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
        var dto = new CreateScriptDto
        {
            Name = name,
            Code = code,
            DataConnectionId = dataConnectionId != null ? Guid.Parse(dataConnectionId) : null
        };

        var script = await api.CreateScriptAsync(dto, cancellationToken);

        if (runImmediately)
        {
            await api.RunScriptInGuiAsync(script.Id, captureOutput: true, cancellationToken);
            var runResult = await api.GetRunOutputAsync(script.Id, wait: true, cancellationToken: cancellationToken);

            var result = new
            {
                Script = script,
                RunResult = runResult
            };

            return JsonSerializer.Serialize(result, JsonDefaults.IndentedOptions);
        }

        return JsonSerializer.Serialize(script, JsonDefaults.IndentedOptions);
    }
}
