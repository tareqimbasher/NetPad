namespace NetPad.ExecutionModel.External;

public class ExternalScriptRunnerOptions
{
    public string[] ProcessCliArgs { get; set; } = [];
    public bool RedirectIo { get; set; }
}
