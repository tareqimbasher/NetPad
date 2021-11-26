using System;

namespace NetPad.Events
{
    public class ScriptConfigPropertyChanged : PropertyChangedEvent
    {
        public ScriptConfigPropertyChanged(Guid scriptId, string propertyName, object? newValue)
            : base(propertyName, newValue)
        {
            ScriptId = scriptId;
        }

        public Guid ScriptId { get; }
    }
}
