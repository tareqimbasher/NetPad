namespace NetPad.Utilities;

/// <summary>
/// A generic wrapper class that wraps a value of type <see cref="T"/>.
/// </summary>
public class Accessor<T>
{
    public Accessor(T value)
    {
        Value = value;
    }

    public T Value { get; private set; }

    public Accessor<T> Update(T newValue)
    {
        Value = newValue;
        return this;
    }
}
