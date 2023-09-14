using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.CQs;

public class GetActiveScriptEnvironmentQuery : Query<ScriptEnvironment?>
{
    public class Handler : IRequestHandler<GetActiveScriptEnvironmentQuery, ScriptEnvironment?>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public Task<ScriptEnvironment?> Handle(GetActiveScriptEnvironmentQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_session.Active);
        }
    }
}
