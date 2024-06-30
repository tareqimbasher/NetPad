using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class OpenScriptCommand : Command
{
    public OpenScriptCommand(string path)
    {
        Path = path;
    }

    public OpenScriptCommand(Script script)
    {
        Script = script;
    }

    public string? Path { get; }
    public Script? Script { get; }


    public class Handler(IScriptRepository scriptRepository, ISession session, IEventBus eventBus)
        : IRequestHandler<OpenScriptCommand>
    {
        public async Task<Unit> Handle(OpenScriptCommand request, CancellationToken cancellationToken)
        {
            Script script;

            if (request.Script != null)
            {
                script = request.Script;
            }
            else if (request.Path != null)
            {
                script = await scriptRepository.GetAsync(request.Path);
            }
            else
            {
                throw new ArgumentException("Not enough information to open a script.");
            }

            await session.OpenAsync(script);

            await eventBus.PublishAsync(new ScriptOpenedEvent(script));

            return Unit.Value;
        }
    }
}
