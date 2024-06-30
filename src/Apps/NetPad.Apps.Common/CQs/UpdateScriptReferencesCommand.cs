using MediatR;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptReferencesCommand(Script script, IEnumerable<Reference> newReferences) : Command
{
    public Script Script { get; } = script;
    public IEnumerable<Reference> NewReferences { get; } = newReferences;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptReferencesCommand>
    {
        public async Task<Unit> Handle(UpdateScriptReferencesCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var newReferences = request.NewReferences;
            var existingReferences = script.Config.References;

            var added = newReferences.Where(newReference => !existingReferences.Contains(newReference)).ToList();
            var removed = existingReferences.Where(e => !newReferences.Contains(e)).ToList();

            script.Config.SetReferences(newReferences);

            await eventBus.PublishAsync(new ScriptReferencesUpdatedEvent(script, added, removed));

            return Unit.Value;
        }
    }
}
