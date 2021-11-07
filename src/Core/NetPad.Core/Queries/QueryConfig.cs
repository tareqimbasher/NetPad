using System.Collections.Generic;

namespace NetPad.Queries
{
    public class QueryConfig
    {
        public QueryConfig(QueryKind kind, List<string> namespaces)
        {
            Kind = kind;
            Namespaces = namespaces;
        }
    
        public QueryKind Kind { get; set; }
        public List<string> Namespaces { get; set; }

        public void SetKind(QueryKind kind)
        {
            if (kind == Kind)
                return;

            Kind = kind;
        }
    }
}