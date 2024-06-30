using System.Text;
using System.Text.Json.Serialization;

namespace NetPad.DotNet;

[method: JsonConstructor]
public record Code(Namespace? Namespace, string? Value) : SourceCodeElement<string?>(Value)
{
    public Code(string? value) : this(null, value)
    {
    }

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
}
