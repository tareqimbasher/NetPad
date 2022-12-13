using System.Collections.Generic;

namespace NetPad.DotNet;

public abstract class SourceCodeElement : ValueObject
{
    public virtual bool Changed { get; protected set; }

    public abstract string ToCodeString();
}

public abstract class SourceCodeElement<TValue> : SourceCodeElement
{
    protected SourceCodeElement(TValue value)
    {
        Value = value;
    }

    public TValue Value { get; private set; }

    public void Update(TValue value)
    {
        Value = value;
        Changed = true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
