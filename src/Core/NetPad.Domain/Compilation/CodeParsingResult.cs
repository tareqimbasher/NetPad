using System.Collections.Generic;

namespace NetPad.Compilation
{
    public class CodeParsingResult
    {
        public CodeParsingResult(string fullProgram, int userCodeStartLine)
        {
            FullProgram = fullProgram;
            UserCodeStartLine = userCodeStartLine;
            Namespaces = new List<string>();
        }

        public string FullProgram { get; }
        public List<string> Namespaces { get; set; }
        public string? UserProgram { get; set; }
        public int UserCodeStartLine { get; }
    }
}
