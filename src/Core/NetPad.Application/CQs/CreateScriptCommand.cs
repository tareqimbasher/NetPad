using MediatR;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.CQs;

public class CreateScriptCommand : Command<Script>
{
    public class Handler : IRequestHandler<CreateScriptCommand, Script>
    {
        private readonly IScriptNameGenerator _scriptNameGenerator;
        private readonly IScriptRepository _scriptRepository;
        private readonly IEventBus _eventBus;

        public Handler(IScriptNameGenerator scriptNameGenerator, IScriptRepository scriptRepository, IEventBus eventBus)
        {
            _scriptNameGenerator = scriptNameGenerator;
            _scriptRepository = scriptRepository;
            _eventBus = eventBus;
        }

        public async Task<Script> Handle(CreateScriptCommand request, CancellationToken cancellationToken)
        {
            var name = _scriptNameGenerator.Generate();
            var script = await _scriptRepository.CreateAsync(name);

            script.Config.SetNamespaces(ScriptConfigDefaults.DefaultNamespaces);

            await _eventBus.PublishAsync(new ScriptCreatedEvent(script));

            return script;
        }
    }
}
