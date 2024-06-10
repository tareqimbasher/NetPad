using MediatR;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class RunScriptCommand(Guid scriptId, RunOptions runOptions) : Command
{
    public Guid ScriptId { get; } = scriptId;
    public RunOptions RunOptions { get; } = runOptions;

    public class Handler(IMediator mediator, IEventBus eventBus) : IRequestHandler<RunScriptCommand>
    {
        public async Task<Unit> Handle(RunScriptCommand request, CancellationToken cancellationToken)
        {
            var environment = await mediator.Send(
                new GetOpenedScriptEnvironmentQuery(request.ScriptId, true));

            await environment!.RunAsync(request.RunOptions);

            await eventBus.PublishAsync(new ScriptRanEvent(environment));

            return Unit.Value;
        }
    }
}
