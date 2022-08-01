using MediatR;
using NetPad.Events;
using NetPad.Runtimes;

namespace NetPad.CQs;

public class RunScriptCommand : Command
{
    public RunScriptCommand(Guid scriptId, RunOptions runOptions)
    {
        ScriptId = scriptId;
        RunOptions = runOptions;
    }

    public Guid ScriptId { get; }
    public RunOptions RunOptions { get; }

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

            await environment!.RunAsync(request.RunOptions);

            await _eventBus.PublishAsync(new ScriptRanEvent(environment));

            return Unit.Value;
        }
    }
}
