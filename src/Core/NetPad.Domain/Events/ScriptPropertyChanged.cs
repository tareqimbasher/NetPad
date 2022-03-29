using System;

namespace NetPad.Events
{
    public class ScriptPropertyChanged : PropertyChangedEvent, IEventWithScriptId
    {
        public ScriptPropertyChanged(Guid scriptId, string propertyName, object? newValue)
            : base(propertyName, newValue)
        {
            ScriptId = scriptId;
        }

        public Guid ScriptId { get; }
    }
}
