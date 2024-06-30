namespace NetPad.Dtos;

public class CreateScriptDto
{
    /// <summary>
    /// The code to include in the newly created script.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// The data connection to set on the newly created script.
    /// </summary>
    public Guid? DataConnectionId { get; set; }

    /// <summary>
    /// If true, will run the script after its created. Only respected if <see cref="Code"/> is set.
    /// </summary>
    public bool RunImmediately { get; set; }
}
