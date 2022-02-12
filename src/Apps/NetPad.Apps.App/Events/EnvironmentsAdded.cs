using NetPad.Scripts;

namespace NetPad.Events
{
    public class EnvironmentsAdded
    {
        public EnvironmentsAdded(params ScriptEnvironment[] environments)
        {
            Environments = environments;
        }

        public ScriptEnvironment[] Environments { get; }
    }
}
