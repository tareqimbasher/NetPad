using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.CQs;

public class GetOpenedScriptEnviornmentsQuery : Query<IEnumerable<ScriptEnvironment>>
{
    public class Handler : IRequestHandler<GetOpenedScriptEnviornmentsQuery, IEnumerable<ScriptEnvironment>>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public Task<IEnumerable<ScriptEnvironment>> Handle(GetOpenedScriptEnviornmentsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<ScriptEnvironment>>(_session.Environments.ToArray());
        }
    }
}
