using MediatR;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class GetOpenedScriptEnvironmentsQuery : Query<IEnumerable<ScriptEnvironment>>
{
    public class Handler(ISession session)
        : IRequestHandler<GetOpenedScriptEnvironmentsQuery, IEnumerable<ScriptEnvironment>>
    {
        public Task<IEnumerable<ScriptEnvironment>> Handle(GetOpenedScriptEnvironmentsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<ScriptEnvironment>>(session.Environments.ToArray());
        }
    }
}
