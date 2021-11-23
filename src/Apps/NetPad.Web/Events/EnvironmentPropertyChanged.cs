using System;

namespace NetPad.Events
{
    public class EnvironmentPropertyChanged
    {
        public EnvironmentPropertyChanged(Guid scriptId, string propertyName, object? newValue)
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
