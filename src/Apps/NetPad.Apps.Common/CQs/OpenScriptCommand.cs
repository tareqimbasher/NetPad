using MediatR;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Apps.CQs;

public class OpenScriptCommand : Command<ScriptEnvironment>
{
    public OpenScriptCommand(Script script)
    {
        Script = script;
    }

    public OpenScriptCommand(Guid id)
    {
        Id = id;
    }

    public OpenScriptCommand(string path)
    {
        Path = path;
    }

    public Script? Script { get; }
    public Guid? Id { get; }
    public string? Path { get; }

    public class Handler(IScriptRepository scriptRepository, ISession session, IEventBus eventBus)
        : IRequestHandler<OpenScriptCommand, ScriptEnvironment>
    {
        public async Task<ScriptEnvironment> Handle(OpenScriptCommand request, CancellationToken cancellationToken)
        {
            Script script;

            if (request.Script != null)
            {
                script = request.Script;
            }
            else if (request.Id != null)
            {
                script = await scriptRepository.GetAsync(request.Id.Value)
                         ?? throw new ScriptNotFoundException(request.Id.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.Path))
            {
                script = await scriptRepository.GetAsync(request.Path);
            }
            else
            {
                throw new ArgumentException("Not enough information to open a script.");
            }

            var environment = await session.OpenAsync(script, true);

            await eventBus.PublishAsync(new ScriptOpenedEvent(script));

            return environment;
        }
    }
}
