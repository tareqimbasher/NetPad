namespace NetPad.ExecutionModel;

/// <summary>
/// Options that configure the running of a script.
/// </summary>
public class RunOptions(string? specificCodeToRun = null)
{
    /// <summary>
    /// If not null, this code will run instead of script code. Typically used to only run code that user has
    /// highlighted in the editor.
    /// </summary>
    public string? SpecificCodeToRun { get; } = specificCodeToRun;
}
