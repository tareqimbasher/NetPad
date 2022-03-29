using System.Collections.Generic;
using NetPad.Scripts;

namespace NetPad.Events
{
    public class ScriptDirectoryChanged : IEvent
    {
        public ScriptDirectoryChanged(IEnumerable<ScriptSummary> scripts)
        {
            Scripts = scripts;
        }

        public IEnumerable<ScriptSummary> Scripts { get; }
    }
}
