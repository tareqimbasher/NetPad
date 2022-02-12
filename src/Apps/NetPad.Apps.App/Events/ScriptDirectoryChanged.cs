using System.Collections.Generic;
using NetPad.Scripts;

namespace NetPad.Events
{
    public class ScriptDirectoryChanged
    {
        public ScriptDirectoryChanged(List<ScriptSummary> scripts)
        {
            Scripts = scripts;
        }

        public List<ScriptSummary> Scripts { get; }
    }
}
