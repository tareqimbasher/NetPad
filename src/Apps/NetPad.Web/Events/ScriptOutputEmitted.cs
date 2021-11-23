using System;

namespace NetPad.Events
{
    public class ScriptOutputEmitted
    {
        public ScriptOutputEmitted(Guid scriptId, string? output)
        {
            ScriptId = scriptId;
            Output = output;
        }

        public Guid ScriptId { get; }
        public string? Output { get; }
    }
}
