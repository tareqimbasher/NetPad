using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class GetActiveScriptEnvironmentQuery : Query<ScriptEnvironment?>
{
    public class Handler(ISession session) : IRequestHandler<GetActiveScriptEnvironmentQuery, ScriptEnvironment?>
    {
        public Task<ScriptEnvironment?> Handle(GetActiveScriptEnvironmentQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(session.Active);
        }
    }
}
