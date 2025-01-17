using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.ClientServer;

/// <summary>
/// A single occurrence of running a script
/// </summary>
internal class ScriptRun(Script script)
{
    private readonly TaskCompletionSource<RunResult> _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly DotNetFrameworkVersion _targetFrameworkVersion = script.Config.TargetFrameworkVersion;
    private readonly Guid? _dataConnectionId = script.DataConnection?.Id;

    public Guid RunId { get; } = Guid.NewGuid();
    public int? UserProgramStartLineNumber { get; set; }
    public bool IsComplete { get; private set; }
    public Task<RunResult> Task => _taskCompletionSource.Task;
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public Guid? DataConnectionId { get; } = script.DataConnection?.Id;

    public bool HasScriptChangedEnoughToRestartScriptHost(Script script)
    {
        return _targetFrameworkVersion != script.Config.TargetFrameworkVersion
               || _dataConnectionId != script.DataConnection?.Id;
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public void SetResult(RunResult result)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        _taskCompletionSource.TrySetResult(result);
    }
}
