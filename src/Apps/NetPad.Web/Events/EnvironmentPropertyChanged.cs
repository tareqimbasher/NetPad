using System;

namespace NetPad.Events
{
    public class EnvironmentPropertyChanged : PropertyChangedEvent
    {
        public EnvironmentPropertyChanged(Guid scriptId, string propertyName, object? newValue)
            : base(propertyName, newValue)
        {
            ScriptId = scriptId;
        }

        public Guid ScriptId { get; }
    }
}
