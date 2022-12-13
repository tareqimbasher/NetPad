using System;
using NetPad.Scripts;

namespace NetPad.Exceptions
{
    public class InvalidScriptFormatException : Exception
    {

        public InvalidScriptFormatException(string scriptName, string message) : base(message)
        {
            ScriptName = scriptName;
        }

        public InvalidScriptFormatException(Script script, string message) : this(script.Name, message)
        {
            Script = script;
        }

        public string ScriptName { get; }
        public Script? Script { get; }
    }
}
