using System.Collections.Generic;

namespace NetPad.Queries
{
    public class QueryConfig
    {
        public QueryConfig()
        {
            QueryKind = QueryKind.Expression;
            Namespaces = new List<string>();
        }
    
        public QueryKind QueryKind { get; private set; }
        public List<string> Namespaces { get; private set; }

        public void SetKind(QueryKind kind)
        {
            if (kind == QueryKind)
                return;

            QueryKind = kind;
        }
    }
}