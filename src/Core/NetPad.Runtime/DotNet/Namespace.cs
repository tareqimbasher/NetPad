namespace NetPad.DotNet;

public record Namespace : SourceCodeElement<string>
{
    public Namespace(string value) : base(Normalize(value))
    {
    }

    public override string ToCodeString() => $"namespace {Value};";

    public static implicit operator Namespace(string value)
    {
        return new Namespace(value);
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

        if (value.StartsWith("namespace"))
        {
            throw new ArgumentException("Cannot start with the keyword 'namespace'", nameof(value));
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
