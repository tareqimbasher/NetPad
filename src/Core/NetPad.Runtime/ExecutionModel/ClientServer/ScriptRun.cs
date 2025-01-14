using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.ClientServer;

/// <summary>
/// A single occurrence of running a script
/// </summary>
internal class ScriptRun(Guid runId, int userProgramStartLineNumber, Script script)
{
    private readonly TaskCompletionSource<RunResult> _taskCompletionSource = new();
    private readonly DotNetFrameworkVersion _targetFrameworkVersion = script.Config.TargetFrameworkVersion;
    private readonly Guid? _dataConnectionId = script.DataConnection?.Id;

    public Guid RunId { get; } = runId;
    public int UserProgramStartLineNumber { get; } = userProgramStartLineNumber;
    public bool IsComplete { get; private set; }
    public Task<RunResult> Task => _taskCompletionSource.Task;

    public Guid? DataConnectionId { get; } = script.DataConnection?.Id;

    public bool HasScriptChangedEnoughToRestartScriptHost(Script script)
    {
        return _targetFrameworkVersion != script.Config.TargetFrameworkVersion
               || _dataConnectionId != script.DataConnection?.Id;
    }

    public void SetResult(RunResult result)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;

        try
        {
            _taskCompletionSource.SetResult(result);
        }
        catch (InvalidOperationException)
        {
            // ignore
        }
    }
}
