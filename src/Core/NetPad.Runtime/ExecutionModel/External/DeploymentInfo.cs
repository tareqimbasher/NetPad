namespace NetPad.ExecutionModel.External;

public record DeploymentInfo(
    string ScriptCompilationFingerprintHash,
    string ScriptAssemblyFileName,
    int UserProgramStartLineNumber)
{
    public DateTime? LastRunAt { get; set; }
}
