using System;
using NetPad.Scripts;

namespace NetPad.Exceptions
{
    public class InvalidScriptFormatException : Exception
    {
        public InvalidScriptFormatException(Script script, string message) : base(message)
        {
            Script = script;
        }

        public Script Script { get; }
    }
}
