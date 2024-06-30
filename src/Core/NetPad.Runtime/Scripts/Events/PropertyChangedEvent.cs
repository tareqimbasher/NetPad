using NetPad.Events;

namespace NetPad.Scripts.Events;

public abstract class PropertyChangedEvent(string propertyName, object? oldValue, object? newValue) : IEvent
{
    public string PropertyName { get; } = propertyName;
    public object? OldValue { get; } = oldValue;
    public object? NewValue { get; } = newValue;
}
