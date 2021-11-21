using System;
using NetPad.Scripts;

namespace NetPad.Exceptions
{
    public class InvalidScriptFormat : Exception
    {
        public InvalidScriptFormat(Script script)
        {
            Script = script;
        }

        public Script Script { get; }
    }
}
