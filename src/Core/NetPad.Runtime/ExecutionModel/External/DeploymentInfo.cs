namespace NetPad.ExecutionModel.External;

public record DeploymentInfo(
    string ScriptCompilationFingerprintHash,
    string ScriptAssemblyFileName,
    int UserProgramStartLineNumber)
{
    public DateTime? LastRunAt { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public string GetScriptName() => ScriptAssemblyFileName.Split("__.dll")[0];
}
