using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptNamespacesCommand(Script script, IEnumerable<string> namespaces) : Command
{
    public Script Script { get; } = script;
    public IEnumerable<string> Namespaces { get; } = namespaces;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptNamespacesCommand>
    {
        public async Task<Unit> Handle(UpdateScriptNamespacesCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var newNamespaces = request.Namespaces;
            var existingNamespaces = script.Config.Namespaces;

            var added = newNamespaces.Where(newNamespace => !existingNamespaces.Contains(newNamespace)).ToList();
            var removed = existingNamespaces.Where(e => !newNamespaces.Contains(e)).ToList();

            script.Config.SetNamespaces(newNamespaces);

            await eventBus.PublishAsync(new ScriptNamespacesUpdatedEvent(script, added, removed));

            return Unit.Value;
        }
    }
}
