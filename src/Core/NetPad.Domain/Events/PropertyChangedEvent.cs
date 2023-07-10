namespace NetPad.Events;

public abstract class PropertyChangedEvent : IEvent
{
    protected PropertyChangedEvent(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}
