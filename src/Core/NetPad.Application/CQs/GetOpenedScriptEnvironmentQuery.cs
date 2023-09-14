using MediatR;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.CQs;

public class GetOpenedScriptEnvironmentQuery : Query<ScriptEnvironment?>
{
    public GetOpenedScriptEnvironmentQuery(Guid scriptId, bool errorIfNotFound = false)
    {
        ScriptId = scriptId;
        ErrorIfNotFound = errorIfNotFound;
    }

    public Guid ScriptId { get; }
    public bool ErrorIfNotFound { get; }

    public class Handler : IRequestHandler<GetOpenedScriptEnvironmentQuery, ScriptEnvironment?>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public Task<ScriptEnvironment?> Handle(GetOpenedScriptEnvironmentQuery request, CancellationToken cancellationToken)
        {
            var environment = _session.Get(request.ScriptId);

            if (environment == null && request.ErrorIfNotFound)
            {
                throw new EnvironmentNotFoundException(request.ScriptId);
            }

            return Task.FromResult(environment);
        }
    }
}
