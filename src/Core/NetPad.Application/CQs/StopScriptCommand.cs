using MediatR;
using NetPad.Events;

namespace NetPad.CQs;

public class StopScriptCommand : Command
{
    public StopScriptCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }

    public class Handler : IRequestHandler<StopScriptCommand>
    {
        private readonly IMediator _mediator;
        private readonly IEventBus _eventBus;

        public Handler(IMediator mediator, IEventBus eventBus)
        {
            _mediator = mediator;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(StopScriptCommand request, CancellationToken cancellationToken)
        {
            var environment = await _mediator.Send(new GetOpenedScriptEnvironmentQuery(request.ScriptId, true));

            await environment!.StopAsync();

            await _eventBus.PublishAsync(new ScriptRunCancelledEvent(environment));

            return Unit.Value;
        }
    }
}
