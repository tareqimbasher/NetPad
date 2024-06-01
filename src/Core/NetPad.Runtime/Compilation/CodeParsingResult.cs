using NetPad.DotNet;

namespace NetPad.Compilation;

public class CodeParsingResult(
    SourceCode userProgram,
    SourceCode bootstrapperProgram,
    SourceCodeCollection? additionalCodeProgram)
{
    private int? _userProgramStartLineNumber;

    public SourceCode UserProgram { get; } = userProgram;
    public SourceCode BootstrapperProgram { get; } = bootstrapperProgram;
    public SourceCodeCollection? AdditionalCodeProgram { get; } = additionalCodeProgram;

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
