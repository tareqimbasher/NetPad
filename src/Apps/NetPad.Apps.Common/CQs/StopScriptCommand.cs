using MediatR;
using NetPad.Events;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class StopScriptCommand(Guid scriptId) : Command
{
    public Guid ScriptId { get; } = scriptId;

    public class Handler(IMediator mediator, IEventBus eventBus) : IRequestHandler<StopScriptCommand>
    {
        public async Task<Unit> Handle(StopScriptCommand request, CancellationToken cancellationToken)
        {
            var environment = await mediator.Send(new GetOpenedScriptEnvironmentQuery(request.ScriptId, true));

            await environment!.StopAsync();

            await eventBus.PublishAsync(new ScriptRunCancelledEvent(environment));

            return Unit.Value;
        }
    }
}
