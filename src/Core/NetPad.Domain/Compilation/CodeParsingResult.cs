using NetPad.DotNet;

namespace NetPad.Compilation;

public class CodeParsingResult
{
    public CodeParsingResult(
        SourceCode userProgram,
        SourceCode bootstrapperProgram,
        SourceCodeCollection? additionalCodeProgram,
        ParsedCodeInformation parsedCodeInformation)
    {
        UserProgram = userProgram;
        BootstrapperProgram = bootstrapperProgram;
        AdditionalCodeProgram = additionalCodeProgram;
        ParsedCodeInformation = parsedCodeInformation;
    }

    public SourceCode UserProgram { get; }
    public SourceCode BootstrapperProgram { get; }
    public SourceCodeCollection? AdditionalCodeProgram { get; }
    public ParsedCodeInformation ParsedCodeInformation { get; }

    public SourceCodeCollection CombineSourceCode()
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

    public string GetFullProgram()
    {
        return CombineSourceCode().ToCodeString();
    }
}
