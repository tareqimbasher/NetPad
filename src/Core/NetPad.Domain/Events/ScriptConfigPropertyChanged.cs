using System;
using NetPad.Scripts;

namespace NetPad.Events
{
    public class ScriptConfigPropertyChanged : PropertyChangedEvent, IScriptEvent
    {
        public ScriptConfigPropertyChanged(Guid scriptId, string propertyName, object? newValue)
            : base(propertyName, newValue)
        {
            ScriptId = scriptId;
        }

        public Guid ScriptId { get; }
    }
}
