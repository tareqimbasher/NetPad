using MediatR;
using NetPad.Events;

namespace NetPad.CQs;

public class RunScriptCommand : Command
{
    public RunScriptCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }

    public class Handler : IRequestHandler<RunScriptCommand>
    {
        private readonly IMediator _mediator;
        private readonly IEventBus _eventBus;

        public Handler(IMediator mediator, IEventBus eventBus)
        {
            _mediator = mediator;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(RunScriptCommand request, CancellationToken cancellationToken)
        {
            var environment = await _mediator.Send(new GetOpenedScriptEnviornmentQuery(request.ScriptId, true));

            await environment!.RunAsync();

            await _eventBus.PublishAsync(new ScriptRanEvent(environment));

            return Unit.Value;
        }
    }
}
