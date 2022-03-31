using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.CQs;

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


    public class Handler : IRequestHandler<OpenScriptCommand>
    {
        private readonly IScriptRepository _scriptRepository;
        private readonly ISession _session;
        private readonly IEventBus _eventBus;

        public Handler(IScriptRepository scriptRepository, ISession session, IEventBus eventBus)
        {
            _scriptRepository = scriptRepository;
            _session = session;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(OpenScriptCommand request, CancellationToken cancellationToken)
        {
            Script script;

            if (request.Script != null)
            {
                script = request.Script;
            }
            else if (request.Path != null)
            {
                script = await _scriptRepository.GetAsync(request.Path);
            }
            else
            {
                throw new ArgumentException("Not enough information to open a script.");
            }

            await _session.OpenAsync(script);

            await _eventBus.PublishAsync(new ScriptOpenedEvent(script));

            return Unit.Value;
        }
    }
}
