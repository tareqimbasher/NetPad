using System;
using NetPad.Scripts;

namespace NetPad.Events
{
    public class ScriptPropertyChanged : PropertyChangedEvent, IScriptEvent
    {
        public ScriptPropertyChanged(Guid scriptId, string propertyName, object? newValue)
            : base(propertyName, newValue)
        {
            ScriptId = scriptId;
        }

        public Guid ScriptId { get; }
    }
}
