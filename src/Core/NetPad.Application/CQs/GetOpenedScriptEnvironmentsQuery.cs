using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.CQs;

public class GetOpenedScriptEnvironmentsQuery : Query<IEnumerable<ScriptEnvironment>>
{
    public class Handler : IRequestHandler<GetOpenedScriptEnvironmentsQuery, IEnumerable<ScriptEnvironment>>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public Task<IEnumerable<ScriptEnvironment>> Handle(GetOpenedScriptEnvironmentsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<ScriptEnvironment>>(_session.Environments.ToArray());
        }
    }
}
