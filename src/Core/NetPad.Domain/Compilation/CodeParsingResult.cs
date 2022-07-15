namespace NetPad.Compilation
{
    public class CodeParsingResult
    {
        public CodeParsingResult(string fullProgram, string userProgram, ParsedCodeInformation parsedCodeInformation)
        {
            FullProgram = fullProgram;
            UserProgram = userProgram;
            ParsedCodeInformation = parsedCodeInformation;
        }

        public string FullProgram { get; }
        public string UserProgram { get; }
        public ParsedCodeInformation ParsedCodeInformation { get; }
    }
}
