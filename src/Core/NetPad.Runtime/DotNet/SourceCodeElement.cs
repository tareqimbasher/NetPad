namespace NetPad.DotNet;

public abstract record SourceCodeElement<TValue>(TValue Value)
{
    protected bool _valueChanged;

    public TValue Value { get; private set; } = Value;

    public virtual bool ValueChanged() => _valueChanged;

    public abstract string ToCodeString();

    public void Update(TValue newValue)
    {
        Value = newValue;
        _valueChanged = true;
    }
}
