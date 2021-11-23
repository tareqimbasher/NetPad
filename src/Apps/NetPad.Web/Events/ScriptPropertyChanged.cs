using System;

namespace NetPad.Events
{
    public class ScriptPropertyChanged
    {
        public ScriptPropertyChanged(Guid scriptId, string propertyName, object? newValue)
        {
            ScriptId = scriptId;
            PropertyName = propertyName;
            NewValue = newValue;
        }

        public Guid ScriptId { get; }
        public string PropertyName { get; }
        public object? NewValue { get; }
    }
}
