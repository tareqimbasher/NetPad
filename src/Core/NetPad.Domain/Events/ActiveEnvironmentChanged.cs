using System;

namespace NetPad.Events
{
    public class ActiveEnvironmentChanged : IEvent
    {
        public ActiveEnvironmentChanged(Guid? scriptId)
        {
            ScriptId = scriptId;
        }

        public Guid? ScriptId { get; }
    }
}
