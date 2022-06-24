using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public interface ISession
    {
        IReadOnlyList<ScriptEnvironment> Environments { get; }
        ScriptEnvironment? Active { get; }

        ScriptEnvironment? Get(Guid scriptId);
        Task OpenAsync(Script script, bool activate = true);
        Task OpenAsync(IEnumerable<Script> scripts);
        Task CloseAsync(Guid scriptId);
        Task ActivateAsync(Guid? scriptId);
        Task ActivateLastActiveScriptAsync();
    }
}
