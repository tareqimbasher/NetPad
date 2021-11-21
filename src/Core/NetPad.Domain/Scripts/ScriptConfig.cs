using System.Collections.Generic;

namespace NetPad.Scripts
{
    public class ScriptConfig
    {
        public ScriptConfig(ScriptKind kind, List<string> namespaces)
        {
            Kind = kind;
            Namespaces = namespaces;
        }

        public ScriptKind Kind { get; set; }
        public List<string> Namespaces { get; set; }

        public void SetKind(ScriptKind kind)
        {
            if (kind == Kind)
                return;

            Kind = kind;
        }
    }
}
