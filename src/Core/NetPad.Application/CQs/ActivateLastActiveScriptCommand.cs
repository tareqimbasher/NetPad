using MediatR;
using NetPad.Sessions;

namespace NetPad.CQs;

public class ActivateLastActiveScriptCommand : Command
{
    public class Handler : IRequestHandler<ActivateLastActiveScriptCommand>
    {
        private readonly ISession _session;

        public Handler(ISession session)
        {
            _session = session;
        }

        public async Task<Unit> Handle(ActivateLastActiveScriptCommand request, CancellationToken cancellationToken)
        {
            await _session.ActivateLastActiveScriptAsync();
            return Unit.Value;
        }
    }
}
