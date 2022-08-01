namespace NetPad.Runtimes;

public class RunOptions
{
    public RunOptions()
    {
    }

    public RunOptions(string? code)
    {
        Code = code;
    }

    public string? Code { get; set; }
}
