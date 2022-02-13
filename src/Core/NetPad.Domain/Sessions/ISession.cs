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
        Task OpenAsync(Script script);
        Task CloseAsync(Guid scriptId);
        Task<string> GetNewScriptNameAsync();
        Task ActivateAsync(Guid? scriptId);
        Task ActivateLastActiveScriptAsync();
    }
}
