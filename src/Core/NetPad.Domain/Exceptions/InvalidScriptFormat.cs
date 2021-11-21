using System;

namespace NetPad.Exceptions
{
    public class InvalidScriptFormat : Exception
    {
        public InvalidScriptFormat(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }
}
