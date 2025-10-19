namespace NetPad.ExecutionModel.External;

public class ExternalScriptRunnerOptions
{
    public bool NoCache { get; set; }
    public bool ForceRebuild { get; set; }
    public string[] ProcessCliArgs { get; set; } = [];
    public bool RedirectIo { get; set; }
}
