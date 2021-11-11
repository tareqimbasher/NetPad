using System;
using System.Collections.ObjectModel;
using NetPad.Queries;

namespace NetPad.Sessions
{
    public interface ISession
    {
        ObservableCollection<Query> OpenQueries { get; }
        Query? Get(Guid id);
        Query? Get(string filePath);
        void Add(Query query);
        void Remove(Guid id);
    }
}
