namespace NetPad.ExecutionModel.ClientServer.Messages;

public record ScriptRunCompleteMessage(
    RunResult Result,
    bool RestartScriptHostOnNextRun,
    string? Error = null);
