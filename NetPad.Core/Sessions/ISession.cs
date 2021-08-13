using System.Collections.ObjectModel;
using NetPad.Queries;

namespace NetPad.Sessions
{
    public interface ISession
    {
        ReadOnlyObservableCollection<Query> OpenQueries { get; }
        void Add(Query query);
        void Remove(Query query);
    }
}