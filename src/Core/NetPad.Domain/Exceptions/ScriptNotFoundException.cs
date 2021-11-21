using System;

namespace NetPad.Exceptions
{
    public class ScriptNotFoundException : Exception
    {
        public ScriptNotFoundException(Guid id) : base($"No script found with id: {id}")
        {
        }

        public ScriptNotFoundException(string path) : base($"No script found with path: {path}")
        {
        }
    }

    public class EnvironmentNotFoundException : Exception
    {
        public EnvironmentNotFoundException(Guid id) : base($"No environment found for script id: {id}")
        {
        }
    }
}
