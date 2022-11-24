using System;
using System.Linq;

namespace NetPad.DotNet;

public class Using : SourceCodeElement<string>
{
    public Using(string value) : base(value)
    {
        Validate(value);
    }

    public override string ToCodeString() => ToCodeString(false);
    public string ToCodeString(bool useGlobalNotation) => $"{(useGlobalNotation ? "global " : "")}using {Value};";

    public static implicit operator Using(string value)
    {
        return new Using(value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is string str)
            return Value == str;

        return base.Equals(obj);
    }

    public override int GetHashCode() => base.GetHashCode();

    public static void Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Cannot be null or whitespace", nameof(value));

        if (value.StartsWith(' ') || value.EndsWith(' '))
            value = value.Trim();

        if (value.StartsWith("using"))
            throw new ArgumentException("Cannot start with the keyword 'using'", nameof(value));

        char firstChar = value.First();

        if (!char.IsLetter(firstChar) && firstChar != '_')
            throw new ArgumentException("Must start with a letter or an underscore", nameof(value));

        if (value.EndsWith(";"))
            throw new ArgumentException("Cannot end with a semi-colon", nameof(value));

        if (value.Contains(' '))
            throw new ArgumentException("Cannot contain spaces", nameof(value));
    }

    public static Using Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Using(text);

        if (text.StartsWith(' ') || text.EndsWith(' '))
            text = text.Trim();

        if (text.StartsWith("using "))
            text = text["using".Length..];

        int ixInvalidChar = Array.FindIndex(text.ToCharArray(), c => !char.IsLetter(c) && c != '_');
        if (ixInvalidChar >= 0)
            text = text[..ixInvalidChar];

        return text;
    }
}
