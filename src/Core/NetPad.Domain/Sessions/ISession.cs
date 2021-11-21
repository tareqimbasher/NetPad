using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public interface ISession : INotifyOnPropertyChanged
    {
        ObservableCollection<ScriptEnvironment> Environments { get; }
        ScriptEnvironment? Active { get; }

        ScriptEnvironment? Get(Guid scriptId);
        Task OpenAsync(Script script);
        Task CloseAsync(Guid scriptId);
        Task<string> GetNewScriptName();
        Task SetActive(Guid? scriptId);
    }
}
