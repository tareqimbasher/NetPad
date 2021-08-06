using System.Collections.Generic;
using NetPad.Queries;

namespace NetPad.Sessions
{
    public class Session
    {
        private readonly HashSet<Query> _openQueries;
        
        public Session()
        {
            _openQueries = new HashSet<Query>();
        }

        public IReadOnlySet<Query> OpenQueries => _openQueries;

        public void Add(Query query)
        {
            if (_openQueries.Contains(query))
                return;

            _openQueries.Add(query);
        }
        
        public void Remove(Query query)
        {
            if (!_openQueries.Contains(query))
                return;

            _openQueries.Remove(query);
        }
    }
}