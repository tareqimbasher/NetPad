using System.Collections.ObjectModel;
using NetPad.Queries;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly ObservableCollection<Query> _openQueries;
        
        public Session()
        {
            _openQueries = new ObservableCollection<Query>();
            OpenQueries = new ReadOnlyObservableCollection<Query>(_openQueries);
        }

        public ReadOnlyObservableCollection<Query> OpenQueries { get; }

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