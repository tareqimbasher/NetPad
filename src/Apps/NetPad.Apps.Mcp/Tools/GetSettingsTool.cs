using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetSettingsTool
{
    [McpServerTool(Name = "get_settings", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Get the current NetPad application settings (read-only). " +
        "Includes: scripts directory path, appearance (theme), editor options, results options, " +
        "OmniSharp configuration, keyboard shortcuts, and custom styles.")]
    public static async Task<string> GetSettings(NetPadApiClient api, CancellationToken cancellationToken)
    {
        return await api.GetSettingsAsync(cancellationToken);
    }
}
