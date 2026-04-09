using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetIlTool
{
    [McpServerTool(Name = "get_il", ReadOnly = true, Destructive = false, Idempotent = true), Description(
         "Get the IL (Intermediate Language) disassembly of an open script. " +
         "The script must be open in the session and have code. " +
         "The script will be compiled and the resulting IL returned. " +
         "Returns a compilation error if the code does not compile.")]
    public static async Task<string> GetIl(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        var il = await api.GetIntermediateLanguageAsync(id, cancellationToken);
        return string.IsNullOrEmpty(il) ? "Script has no code or produced no IL." : il;
    }
}
