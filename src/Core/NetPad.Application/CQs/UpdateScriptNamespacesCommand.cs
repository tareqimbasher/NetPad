using MediatR;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.CQs;

public class UpdateScriptNamespacesCommand : Command
{
    public UpdateScriptNamespacesCommand(Script script, IEnumerable<string> namespaces)
    {
        Script = script;
        Namespaces = namespaces;
    }

    public Script Script { get; }
    public IEnumerable<string> Namespaces { get; }

    public class Handler : IRequestHandler<UpdateScriptNamespacesCommand>
    {
        private readonly IEventBus _eventBus;

        public Handler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(UpdateScriptNamespacesCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var newNamespaces = request.Namespaces;
            var existingNamespaces = script.Config.Namespaces;

            var added = newNamespaces.Where(newNamespace => !existingNamespaces.Contains(newNamespace)).ToList();
            var removed = existingNamespaces.Where(e => !newNamespaces.Contains(e)).ToList();

            script.Config.SetNamespaces(newNamespaces);

            await _eventBus.PublishAsync(new ScriptNamespacesUpdatedEvent(script, added, removed));

            return Unit.Value;
        }
    }
}
