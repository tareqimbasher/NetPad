using MediatR;

namespace NetPad.Apps.CQs;

public class StopScriptCommand(Guid scriptId, bool stopRunner) : Command
{
    public Guid ScriptId { get; } = scriptId;

    /// <summary>
    /// If true, will stop the script runner even if the script itself is not currently running.
    /// The runner is always stopped regardless of the value of this param if the script is
    /// currently running.
    /// </summary>
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
