namespace NetPad.ExecutionModel.External.Interface;

/// <summary>
/// Info about current script.
/// </summary>
/// <param name="Id">The unique script Id.</param>
/// <param name="Name">The script name.</param>
/// <param name="Location">The full path of the script file.</param>
public record UserScript(Guid Id, string Name, string Location);
