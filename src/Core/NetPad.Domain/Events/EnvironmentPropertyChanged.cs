using System;
using NetPad.Scripts;

namespace NetPad.Events
{
    public class EnvironmentPropertyChanged : PropertyChangedEvent, IScriptEvent
    {
        public EnvironmentPropertyChanged(Guid scriptId, string propertyName, object? newValue)
            : base(propertyName, newValue)
        {
            ScriptId = scriptId;
        }

        public Guid ScriptId { get; }
    }
}
