using System.Collections.Generic;

namespace NetPad.Compilation
{
    public class CodeParsingResult
    {

        public CodeParsingResult(string program)
        {
            Program = program;
            Namespaces = new List<string>();
        }

        public string Program { get; }
        public List<string> Namespaces { get; set; }
        public string? UserProgram { get; set; }
    }
}
