using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class ListScriptsTool
{
    [McpServerTool(Name = "list_scripts", ReadOnly = true), Description(
        "List all scripts — both currently open in NetPad and saved on disk. " +
        "Open scripts include their execution status.")]
    public static async Task<string> ListScripts(NetPadApiClient api, CancellationToken cancellationToken)
    {
        var envsTask = api.GetEnvironmentsAsync(cancellationToken);
        var allTask = api.GetAllScriptsAsync(cancellationToken);
        await Task.WhenAll(envsTask, allTask);

        var environments = envsTask.Result;
        var allScripts = allTask.Result;
        var openScriptIds = new HashSet<Guid>(environments.Select(e => e.Script.Id));

        var result = new
        {
            Open = environments.Select(e => new
            {
                e.Script.Id,
                e.Script.Name,
                e.Script.Path,
                e.Status,
                e.RunDurationMilliseconds
            }),
            Saved = allScripts
                .Where(s => !openScriptIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name, s.Path })
        };

        return JsonSerializer.Serialize(result, JsonDefaults.IndentedOptions);
    }
}
