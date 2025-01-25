using MediatR;

namespace NetPad.Apps.CQs;

public class StopScriptCommand(Guid scriptId, bool stopRunner) : Command
{
    public Guid ScriptId { get; } = scriptId;
    public bool StopRunner { get; } = stopRunner;

    public class Handler(IMediator mediator) : IRequestHandler<StopScriptCommand>
    {
        public async Task<Unit> Handle(StopScriptCommand request, CancellationToken cancellationToken)
        {
            var environment = await mediator.Send(
                new GetOpenedScriptEnvironmentQuery(request.ScriptId, true),
                cancellationToken);

            await environment!.StopAsync(request.StopRunner);

            return Unit.Value;
        }
    }
}
