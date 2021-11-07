using System;

namespace NetPad.Exceptions
{
    public class InvalidQueryFormat : Exception
    {
        public InvalidQueryFormat(string filePath)
        {
            FilePath = filePath;
        }
        
        public string FilePath { get; }
    }
}