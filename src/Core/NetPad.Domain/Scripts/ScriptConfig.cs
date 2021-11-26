using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetPad.Common;

namespace NetPad.Scripts
{
    public class ScriptConfig : INotifyOnPropertyChanged
    {
        private ScriptKind _kind;
        private List<string> _namespaces;

        public ScriptConfig(ScriptKind kind, List<string>? namespaces = null)
        {
            _kind = kind;
            _namespaces = namespaces ?? new List<string>();
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        [JsonIgnore] public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }

        public ScriptKind Kind
        {
            get => _kind;
            private set => this.RaiseAndSetIfChanged(ref _kind, value);
        }

        public List<string> Namespaces
        {
            get => _namespaces;
            private set => this.RaiseAndSetIfChanged(ref _namespaces, value);
        }

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
