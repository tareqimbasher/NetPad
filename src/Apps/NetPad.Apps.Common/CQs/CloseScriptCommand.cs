using MediatR;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class CloseScriptCommand(Guid scriptId) : Command
{
    public Guid ScriptId { get; } = scriptId;

    public class Handler(
        ISession session,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        IEventBus eventBus)
        : IRequestHandler<CloseScriptCommand>
    {
        public async Task<Unit> Handle(CloseScriptCommand request, CancellationToken cancellationToken)
        {
            var scriptId = request.ScriptId;

            var script = session.Get(scriptId)?.Script ?? throw new ScriptNotFoundException(scriptId);

            await session.CloseAsync(scriptId);

            await autoSaveScriptRepository.DeleteAsync(script);

            await eventBus.PublishAsync(new ScriptClosedEvent(script));

            return Unit.Value;
        }
    }
}
