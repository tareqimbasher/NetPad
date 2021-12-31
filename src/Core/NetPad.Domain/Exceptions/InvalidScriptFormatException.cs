using System;
using NetPad.Scripts;

namespace NetPad.Exceptions
{
    public class InvalidScriptFormatException : Exception
    {
        public InvalidScriptFormatException(Script script)
        {
            Script = script;
        }

        public Script Script { get; }
    }
}
