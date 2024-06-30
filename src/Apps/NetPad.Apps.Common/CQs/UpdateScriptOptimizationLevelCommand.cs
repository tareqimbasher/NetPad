using MediatR;
using Microsoft.CodeAnalysis;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptOptimizationLevelCommand(Script script, OptimizationLevel optimizationLevel) : Command
{
    public Script Script { get; } = script;
    public OptimizationLevel OptimizationLevel { get; } = optimizationLevel;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptOptimizationLevelCommand>
    {
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

            await eventBus.PublishAsync(new ScriptOptimizationLevelUpdatedEvent(script, oldOptimizationLevel, newOptimizationLevel));

            return Unit.Value;
        }
    }
}
