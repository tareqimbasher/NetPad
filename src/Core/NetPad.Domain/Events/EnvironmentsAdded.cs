using NetPad.Scripts;

namespace NetPad.Events
{
    public class EnvironmentsAdded : IEvent
    {
        public EnvironmentsAdded(params ScriptEnvironment[] environments)
        {
            Environments = environments;
        }

        public ScriptEnvironment[] Environments { get; }
    }
}
