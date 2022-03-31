using MediatR;
using NetPad.Sessions;

namespace NetPad.CQs;

public class ActivateScriptCommand : Command
{
    public ActivateScriptCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }

    public class Handler : IRequestHandler<ActivateScriptCommand>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public async Task<Unit> Handle(ActivateScriptCommand request, CancellationToken cancellationToken)
        {
            await _session.ActivateAsync(request.ScriptId);
            return Unit.Value;;
        }
    }
}
