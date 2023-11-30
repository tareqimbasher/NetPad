using System;

namespace O2Html;

/// <summary>
/// An exception that is thrown when there is a problem serializing a .NET object or value type to HTML.
/// </summary>
public class HtmlSerializationException : Exception
{
    public HtmlSerializationException(string message) : base(message)
    {
    }
}
