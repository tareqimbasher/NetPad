using System;
using System.Collections.ObjectModel;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public interface ISession
    {
        ObservableCollection<Script> OpenScripts { get; }
        Script? Get(Guid id);
        Script? Get(string filePath);
        void Add(Script script);
        void Remove(Guid id);
    }
}
