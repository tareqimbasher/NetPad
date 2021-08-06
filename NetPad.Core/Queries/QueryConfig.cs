namespace NetPad.Queries
{
    public class QueryConfig
    {
        public QueryConfig()
        {
            QueryKind = QueryKind.Expression;
        }
    
        public QueryKind QueryKind { get; private set; }

        public void SetKind(QueryKind kind)
        {
            if (kind == QueryKind)
                return;

            QueryKind = kind;
        }
    }
}