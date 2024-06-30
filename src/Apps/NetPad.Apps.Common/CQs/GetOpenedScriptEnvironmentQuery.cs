using MediatR;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class GetOpenedScriptEnvironmentQuery(Guid scriptId, bool errorIfNotFound = false) : Query<ScriptEnvironment?>
{
    public Guid ScriptId { get; } = scriptId;
    public bool ErrorIfNotFound { get; } = errorIfNotFound;

    public class Handler(ISession session) : IRequestHandler<GetOpenedScriptEnvironmentQuery, ScriptEnvironment?>
    {
        public Task<ScriptEnvironment?> Handle(GetOpenedScriptEnvironmentQuery request, CancellationToken cancellationToken)
        {
            var environment = session.Get(request.ScriptId);

            if (environment == null && request.ErrorIfNotFound)
            {
                throw new EnvironmentNotFoundException(request.ScriptId);
            }

            return Task.FromResult(environment);
        }
    }
}
