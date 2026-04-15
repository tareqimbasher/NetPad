using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class DeleteScriptCommand(Script script) : Command
{
    public Script Script { get; } = script;

    public class Handler(
        ISession session,
        IScriptRepository scriptRepository,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        IEventBus eventBus)
        : IRequestHandler<DeleteScriptCommand>
    {
        public async Task<Unit> Handle(DeleteScriptCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;

            if (session.IsOpen(script.Id))
            {
                await session.CloseAsync(script.Id);
                await autoSaveScriptRepository.DeleteAsync(script);
                await eventBus.PublishAsync(new ScriptClosedEvent(script));
            }

            await scriptRepository.DeleteAsync(script);

            return Unit.Value;
        }
    }
}
