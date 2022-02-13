namespace NetPad.Events
{
    public abstract class PropertyChangedEvent : IEvent
    {
        protected PropertyChangedEvent(string propertyName, object? newValue)
        {
            PropertyName = propertyName;
            NewValue = newValue;
        }

        public string PropertyName { get; }
        public object? NewValue { get; }
    }
}
