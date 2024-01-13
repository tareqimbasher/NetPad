using MediatR;
using Microsoft.CodeAnalysis;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.CQs;

public class UpdateScriptOptimizationLevelCommand : Command
{

    public UpdateScriptOptimizationLevelCommand(Script script, OptimizationLevel optimizationLevel)
    {
        Script = script;
        OptimizationLevel = optimizationLevel;
    }

    public Script Script { get; }
    public OptimizationLevel OptimizationLevel { get; }

    public class Handler : IRequestHandler<UpdateScriptOptimizationLevelCommand>
    {
        private readonly IEventBus _eventBus;

        public Handler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(UpdateScriptOptimizationLevelCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var oldOptimizationLevel = script.Config.OptimizationLevel;
            var newOptimizationLevel = request.OptimizationLevel;

            if (oldOptimizationLevel == newOptimizationLevel)
            {
                return Unit.Value;
            }

            script.Config.SetOptimizationLevel(newOptimizationLevel);

            await _eventBus.PublishAsync(new ScriptOptimizationLevelUpdatedEvent(script, oldOptimizationLevel, newOptimizationLevel));

            return Unit.Value;
        }
    }
}
