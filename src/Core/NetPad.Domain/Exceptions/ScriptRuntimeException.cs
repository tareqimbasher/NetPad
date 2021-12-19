using System;

namespace NetPad.Exceptions
{
    public class ScriptRuntimeException : Exception
    {
        public ScriptRuntimeException(string message) : base(message)
        {
        }
    }
}
