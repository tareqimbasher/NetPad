using System;

namespace NetPad.Exceptions;

public class EnvironmentNotFoundException : Exception
{
    public EnvironmentNotFoundException(Guid scriptId) : base($"No environment found for script id: {scriptId}")
    {
    }
}
