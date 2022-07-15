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
        private List<Reference> _references;

        public ScriptConfig(
            ScriptKind kind,
            List<string>? namespaces = null,
            List<Reference>? references = null)
        {
            _kind = kind;
            _namespaces = namespaces ?? new List<string>();
            _references = references ?? new List<Reference>();
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

        public List<Reference> References
        {
            get => _references;
            private set => this.RaiseAndSetIfChanged(ref _references, value);
        }

        public void SetKind(ScriptKind kind)
        {
            if (kind == Kind)
                return;

            Kind = kind;
        }

        public void SetNamespaces(IEnumerable<string> namespaces)
        {
            if (Namespaces.SequenceEqual(namespaces))
                return;

            namespaces = namespaces
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Select(ns => ns.Trim());

            if (namespaces.Any(ns => ns.StartsWith("using ") || ns.EndsWith(';')))
            {
                throw new ArgumentException("Namespaces should not start with 'using ' and must not end with ';'");
            }

            Namespaces = namespaces.Distinct().ToList();
        }

        public void SetReferences(IEnumerable<Reference> references)
        {
            if (References.SequenceEqual(references))
                return;

            foreach (var reference in references)
                reference.EnsureValid();

            References = references.ToList();
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
