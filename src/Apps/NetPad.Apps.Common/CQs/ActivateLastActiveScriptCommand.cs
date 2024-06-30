using MediatR;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class ActivateLastActiveScriptCommand : Command
{
    public class Handler(ISession session) : IRequestHandler<ActivateLastActiveScriptCommand>
    {
        public async Task<Unit> Handle(ActivateLastActiveScriptCommand request, CancellationToken cancellationToken)
        {
            await session.ActivateLastActiveScriptAsync();
            return Unit.Value;
        }
    }
}
