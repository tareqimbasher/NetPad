using NetPad.DotNet;

namespace NetPad.Exceptions;

public class InvalidReferenceException(Reference reference, string message) : Exception(message)
{
    public Reference Reference { get; } = reference;
}
