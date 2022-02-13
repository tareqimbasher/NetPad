using NetPad.Scripts;

namespace NetPad.Events
{
    public class EnvironmentsRemoved : IEvent
    {
        public EnvironmentsRemoved(params ScriptEnvironment[] environments)
        {
            Environments = environments;
        }

        public ScriptEnvironment[] Environments { get; }
    }
}
