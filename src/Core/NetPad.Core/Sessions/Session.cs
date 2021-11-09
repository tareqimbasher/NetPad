using System.Collections.ObjectModel;
using System.Linq;
using NetPad.Queries;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly ObservableCollection<Query> _openQueries;

        public Session()
        {
            _openQueries = new ObservableCollection<Query>();
        }

        public ObservableCollection<Query> OpenQueries => _openQueries;

        public void Add(Query query)
        {
            if (_openQueries.Contains(query) ||
                _openQueries.Any(q => q.FilePath == query.FilePath || (q.IsNew && q.Name == query.Name)))
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
