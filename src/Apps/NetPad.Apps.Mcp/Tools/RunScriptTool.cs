using System.ComponentModel;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class RunScriptTool
{
    [McpServerTool(Name = "run_script"), Description(
        "Run an existing script in NetPad by its ID or name. " +
        "If the script is open in the GUI, it runs through the normal GUI flow " +
        "(status updates, output visible in GUI). Otherwise it runs headlessly. " +
        "Either scriptId or name must be provided.")]
    public static async Task<string> RunScript(
        NetPadApiClient api,
        [Description("Script ID (GUID). If both scriptId and name are provided, scriptId takes precedence.")] string? scriptId = null,
        [Description("Script name to find and run. Used only if scriptId is not provided.")] string? name = null,
        [Description("Optional execution timeout in milliseconds")] int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        Guid id;

        if (scriptId != null)
        {
            if (!Guid.TryParse(scriptId, out id))
                return "Invalid scriptId format. Expected a GUID.";
        }
        else if (name != null)
        {
            var scripts = await api.FindScriptsAsync(name, cancellationToken);

            if (scripts.Length == 0)
            {
                return $"No script found matching '{name}'.";
            }

            if (scripts.Length > 1)
            {
                var names = string.Join(", ", scripts.Select(s => $"{s.Name} ({s.Id})"));
                return $"Multiple scripts match '{name}': {names}. Please specify a scriptId.";
            }

            id = scripts[0].Id;
        }
        else
        {
            return "Either scriptId or name must be provided.";
        }

        // If the script is open in the GUI, run through the GUI path so the
        // ScriptEnvironment status updates are visible (statusbar, run indicators).
        var environments = await api.GetEnvironmentsAsync(cancellationToken);
        var isOpen = environments.Any(e => e.Script.Id == id);

        HeadlessRunResult result;

        if (isOpen)
        {
            await api.RunScriptInGuiAsync(id, captureOutput: true, cancellationToken);
            result = await api.GetRunOutputAsync(id, wait: true, timeoutMs: timeoutMs, cancellationToken);
        }
        else
        {
            result = await api.RunScriptAsync(id, timeoutMs, cancellationToken);
        }

        return ExecutionResultFormatter.Format(result);
    }
}
