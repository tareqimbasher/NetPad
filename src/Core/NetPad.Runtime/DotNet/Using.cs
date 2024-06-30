namespace NetPad.DotNet;

public record Using : SourceCodeElement<string>
{
    public Using(string value) : base(Normalize(value))
    {
    }

    public override string ToCodeString() => ToCodeString(false);
    public string ToCodeString(bool useGlobalNotation) => $"{(useGlobalNotation ? "global " : "")}using {Value};";

    public static implicit operator Using(string value)
    {
        return new Using(value);
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cannot be null or whitespace", nameof(value));
        }

        if (value.StartsWith(' ') || value.EndsWith(' '))
        {
            value = value.Trim();
        }

        if (value.StartsWith("using"))
        {
            throw new ArgumentException("Cannot start with the keyword 'using'", nameof(value));
        }

        char firstChar = value[0];

        if (!char.IsLetter(firstChar) && firstChar != '_')
        {
            throw new ArgumentException("Must start with a letter or an underscore", nameof(value));
        }

        if (value.EndsWith(";"))
        {
            throw new ArgumentException("Cannot end with a semi-colon", nameof(value));
        }

        return value.ReplaceLineEndings(string.Empty);
    }
}
