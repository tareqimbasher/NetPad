using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.CQs;

public class GetActiveScriptEnviornmentQuery : Query<ScriptEnvironment?>
{
    public class Handler : IRequestHandler<GetActiveScriptEnviornmentQuery, ScriptEnvironment?>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public Task<ScriptEnvironment?> Handle(GetActiveScriptEnviornmentQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_session.Active);
        }
    }
}
