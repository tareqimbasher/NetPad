using MediatR;
using Microsoft.Extensions.Logging;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class StopAllScriptsCommand(bool stopRunner) : Command
{
    /// <summary>
    /// If true, stops the runner script-host process even if the script is not running.
    /// </summary>
    public bool StopRunner { get; } = stopRunner;

    public class Handler(ISession session, ILogger<StopAllScriptsCommand> logger)
        : IRequestHandler<StopAllScriptsCommand>
    {
        public async Task<Unit> Handle(StopAllScriptsCommand request, CancellationToken cancellationToken)
        {
            IEnumerable<ScriptEnvironment> environments = session.GetOpened();

            if (!request.StopRunner)
            {
                environments = environments.Where(x => x.Status == ScriptStatus.Running);
            }

            await Task.WhenAll(environments.Select(async environment =>
            {
                try
                {
                    await environment.StopAsync(request.StopRunner);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An error occured while stopping script: {Script}", environment.Script);
                }
            }).ToArray());

            return Unit.Value;
        }
    }
}
