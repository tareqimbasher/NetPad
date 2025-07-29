using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Compilation;

/// <summary>
/// The result of parsing user script code.
/// </summary>
public class CodeParsingResult(
    SourceCode userProgram,
    SourceCode bootstrapperProgram,
    SourceCodeCollection? additionalCodeProgram)
{
    private int? _userProgramStartLineNumber;

    /// <summary>
    /// User script code converted to a full runnable program.
    /// </summary>
    public SourceCode UserProgram { get; } = userProgram;

    /// <summary>
    /// Code that configures and prepares process before <see cref="UserProgram"/> is executed.
    /// </summary>
    public SourceCode BootstrapperProgram { get; } = bootstrapperProgram;

    /// <summary>
    /// Additional code to add to the runnable program.
    /// </summary>
    public SourceCodeCollection? AdditionalCodeProgram { get; } = additionalCodeProgram;

    /// <summary>
    /// The line number on which the <see cref="UserProgram"/> code starts.
    /// </summary>
    public int UserProgramStartLineNumber
    {
        get
        {
            if (_userProgramStartLineNumber == null)
            {
                _userProgramStartLineNumber = GetFullProgram().GetAllUsings().Count + 1;
            }

            return _userProgramStartLineNumber.Value;
        }
    }

    /// <summary>
    /// Combines <see cref="UserProgram"/>, <see cref="BootstrapperProgram"/> and <see cref="AdditionalCodeProgram"/>
    /// into a full runnable program.
    /// </summary>
    public SourceCodeCollection GetFullProgram()
    {
        var combined = new SourceCodeCollection();

        combined.Add(UserProgram);

        if (AdditionalCodeProgram != null)
        {
            combined.AddRange(AdditionalCodeProgram);
        }

        combined.Add(BootstrapperProgram);

        return combined;
    }
}
