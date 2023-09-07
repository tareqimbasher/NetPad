using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetPad.DotNet;

public class Code : SourceCodeElement<string?>
{
    public Code(string? value) : this(null, value)
    {
    }

    [JsonConstructor]
    public Code(Namespace? @namespace, string? value) : base(value)
    {
        Namespace = @namespace;
    }

    public Namespace? Namespace { get; }

    public override bool ValueChanged()
    {
        return _valueChanged || (Namespace != null && Namespace.ValueChanged());
    }

    public override string ToCodeString()
    {
        var sb = new StringBuilder();

        if (Namespace != null)
        {
            sb.AppendLine(Namespace.ToCodeString()).AppendLine();
        }

        if (Value != null)
        {
            sb.AppendLine(Value);
        }

        return sb.ToString();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return base.GetEqualityComponents();
        yield return Namespace;
    }
}
