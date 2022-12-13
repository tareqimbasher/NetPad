using System.Collections.Generic;
using System.Text;

namespace NetPad.DotNet;

public class Code : SourceCodeElement<string?>
{
    private bool _changed;

    public Code(string? value) : this(null, value)
    {
    }

    public Code(Namespace? @namespace, string? value) : base(value)
    {
        Namespace = @namespace;
    }

    public Namespace? Namespace { get; }

    public override bool Changed
    {
        get => _changed || (Namespace != null && Namespace.Changed);
        protected set => _changed = value;
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
