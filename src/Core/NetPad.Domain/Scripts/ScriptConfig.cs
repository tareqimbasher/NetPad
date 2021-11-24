using System.Collections.Generic;
using System.Linq;

namespace NetPad.Scripts
{
    public class ScriptConfig
    {
        public ScriptConfig(ScriptKind kind, List<string> namespaces)
        {
            Kind = kind;
            Namespaces = namespaces;
        }

        public ScriptKind Kind { get; private set; }
        public List<string> Namespaces { get; private set; }

        public void SetKind(ScriptKind kind)
        {
            if (kind == Kind)
                return;

            Kind = kind;
        }

        public void SetNamespaces(IEnumerable<string> namespaces)
        {
            Namespaces = namespaces.Distinct().ToList();
        }
    }

    public static class ScriptConfigDefaults
    {
        public static readonly List<string> DefaultNamespaces = new List<string>
        {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            //"System.Data",
            "System.Diagnostics",
            "System.IO",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Reflection",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Threading",
            "System.Threading.Tasks",
            //"System.Transactions",
            "System.Xml",
            "System.Xml.Linq",
            "System.Xml.XPath",
        };
    }
}
