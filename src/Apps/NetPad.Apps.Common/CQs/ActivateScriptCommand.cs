using MediatR;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class ActivateScriptCommand(Guid scriptId) : Command
{
    public Guid ScriptId { get; } = scriptId;

    public class Handler(ISession session) : IRequestHandler<ActivateScriptCommand>
    {
        public async Task<Unit> Handle(ActivateScriptCommand request, CancellationToken cancellationToken)
        {
            await session.ActivateAsync(request.ScriptId);
            return Unit.Value;
        }
    }
}
