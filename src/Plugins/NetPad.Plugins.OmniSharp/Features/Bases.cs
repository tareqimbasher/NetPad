using NetPad.Apps.CQs;

namespace NetPad.Plugins.OmniSharp.Features;

public interface ITargetSpecificOmniSharpServer
{
    Guid ScriptId { get; }
}

public abstract class OmniSharpScriptQuery<TResponse>(Guid scriptId) : Query<TResponse>, ITargetSpecificOmniSharpServer
{
    public Guid ScriptId { get; } = scriptId;
}

public abstract class OmniSharpScriptQuery<TInput, TResponse>(Guid scriptId, TInput input) : OmniSharpScriptQuery<TResponse>(scriptId)
{
    public TInput Input { get; } = input;
}

public abstract class OmniSharpScriptCommand(Guid scriptId) : Command, ITargetSpecificOmniSharpServer
{
    public Guid ScriptId { get; } = scriptId;
}

public abstract class OmniSharpScriptCommand<TResponse>(Guid scriptId) : Command<TResponse>, ITargetSpecificOmniSharpServer
{
    public Guid ScriptId { get; } = scriptId;
}

public abstract class OmniSharpScriptCommand<TInput, TResponse>(Guid scriptId, TInput input) : OmniSharpScriptCommand<TResponse>(scriptId)
{
    public TInput Input { get; } = input;
}
