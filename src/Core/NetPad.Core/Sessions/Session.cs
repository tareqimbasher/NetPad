using System;
using System.Collections.ObjectModel;
using System.Linq;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly ObservableCollection<Script> _openScripts;

        public Session()
        {
            _openScripts = new ObservableCollection<Script>();
        }

        public ObservableCollection<Script> OpenScripts => _openScripts;

        public Script? Get(Guid id)
        {
            return _openScripts.FirstOrDefault(q => q.Id == id);
        }

        public Script? Get(string filePath)
        {
            return _openScripts.FirstOrDefault(q => q.FilePath == filePath);
        }

        public void Add(Script script)
        {
            if (_openScripts.Contains(script) ||
                _openScripts.Any(q => (!q.IsNew && q.FilePath == script.FilePath) || (q.IsNew && q.Name == script.Name)))
                return;

            _openScripts.Add(script);
        }

        public void Remove(Guid id)
        {
            var script = _openScripts.FirstOrDefault(q => q.Id == id);

            if (script != null)
                _openScripts.Remove(script);
        }
    }
}
