using Microsoft.CodeAnalysis;
using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptOptimizationLevelUpdatedEvent(Script script, OptimizationLevel oldValue, OptimizationLevel newValue)
    : IEvent
{
    public Script Script { get; } = script;
    public OptimizationLevel OldValue { get; } = oldValue;
    public OptimizationLevel NewValue { get; } = newValue;
}
