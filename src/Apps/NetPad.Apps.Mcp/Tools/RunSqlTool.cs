using System.ComponentModel;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class RunSqlTool
{
    [McpServerTool(Name = "run_sql"), Description(
        "Run SQL code against a database connection configured in NetPad. " +
        "Use list_data_connections to find available connection IDs.")]
    public static async Task<string> RunSql(
        NetPadApiClient api,
        [Description("The SQL code to execute")] string code,
        [Description("Data connection ID (GUID) to execute the SQL against")] string dataConnectionId,
        [Description("Optional execution timeout in milliseconds")] int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var request = new HeadlessRunRequest
        {
            Code = code,
            Kind = HeadlessRunRequest.KindSql,
            DataConnectionId = Guid.Parse(dataConnectionId),
            TimeoutMs = timeoutMs
        };

        var result = await api.RunCodeAsync(request, cancellationToken);
        return ExecutionResultFormatter.Format(result);
    }
}
