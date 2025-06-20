namespace NetPad.Utilities;

/// <summary>
/// Provides a mutable wrapper around a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value being encapsulated.</typeparam>
public class Accessor<T>(T value)
{
    /// <summary>
    /// Gets the current value stored in the accessor.
    /// </summary>
    public T Value { get; private set; } = value;

    /// <summary>
    /// Updates the stored value to <paramref name="newValue"/> and returns this accessor for fluent chaining.
    /// </summary>
    /// <remarks>
    ///   This method mutates the accessor in place. For example:
    /// <code language="csharp">
    /// var userCount = new Accessor(0);
    /// userCount.Update(5).Update(10);
    /// Console.WriteLine(userCount.Value); // 10
    /// </code>
    /// </remarks>
    public Accessor<T> Update(T newValue)
    {
        Value = newValue;
        return this;
    }
}
