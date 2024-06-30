namespace NetPad.ExecutionModel.External;

public class ExternalScriptRunnerOptions(string[] processCliArgs, bool redirectIo)
{
    public string[] ProcessCliArgs { get; set; } = processCliArgs;
    public bool RedirectIo { get; set; } = redirectIo;
}
