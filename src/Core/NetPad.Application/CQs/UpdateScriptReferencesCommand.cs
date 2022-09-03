using MediatR;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.CQs;

public class UpdateScriptReferencesCommand : Command
{
    public UpdateScriptReferencesCommand(Script script, IEnumerable<Reference> newReferences)
    {
        Script = script;
        NewReferences = newReferences;
    }

    public Script Script { get; }
    public IEnumerable<Reference> NewReferences { get; }

    public class Handler : IRequestHandler<UpdateScriptReferencesCommand>
    {
        private readonly IEventBus _eventBus;

        public Handler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(UpdateScriptReferencesCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var newReferences = request.NewReferences;
            var existingReferences = script.Config.References;

            var added = newReferences.Where(newReference => !existingReferences.Contains(newReference)).ToList();
            var removed = existingReferences.Where(e => !newReferences.Contains(e)).ToList();

            script.Config.SetReferences(newReferences);

            await _eventBus.PublishAsync(new ScriptReferencesUpdatedEvent(script, added, removed));

            return Unit.Value;
        }
    }
}
