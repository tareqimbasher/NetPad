namespace NetPad.ExecutionModel.ClientServer.Messages;

public record RunScriptMessage(
    Guid RunId,
    Guid ScriptId,
    string ScriptName,
    string? ScriptFilePath,
    bool IsDirty,
    string ScriptHostDepDirPath,
    string ScriptDirPath,
    string ScriptAssemblyPath,
    string[] ProbingPaths
);
