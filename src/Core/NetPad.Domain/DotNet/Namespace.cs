using System;
using System.Linq;

namespace NetPad.DotNet;

public class Namespace : SourceCodeElement<string>
{
    public Namespace(string value) : base(value)
    {
        Validate(value);
    }

    public override string ToCodeString() => $"namespace {Value};";

    public static implicit operator Namespace(string value)
    {
        return new Namespace(value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is string str)
            return Value == str;

        return base.Equals(obj);
    }

    public static void Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Cannot be null or whitespace", nameof(value));

        if (value.StartsWith(' ') || value.EndsWith(' '))
            value = value.Trim();

        if (value.StartsWith("namespace"))
            throw new ArgumentException("Cannot start with the keyword 'namespace'", nameof(value));

        char firstChar = value.First();

        if (!char.IsLetter(firstChar) && firstChar != '_')
            throw new ArgumentException("Must start with a letter or an underscore", nameof(value));

        if (value.EndsWith(";"))
            throw new ArgumentException("Cannot end with a semi-colon", nameof(value));

        if (value.Contains(' '))
            throw new ArgumentException("Cannot contain spaces", nameof(value));
    }

    public static Namespace Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Namespace(text);

        if (text.StartsWith(' ') || text.EndsWith(' '))
            text = text.Trim();

        if (text.StartsWith("namespace "))
            text = text["namespace".Length..];

        int ixInvalidChar = Array.FindIndex(text.ToCharArray(), c => !char.IsLetter(c) && c != '_');
        if (ixInvalidChar >= 0)
            text = text[..ixInvalidChar];

        return text;
    }
}
