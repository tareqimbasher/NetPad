using NetPad.CQs;

namespace NetPad.Plugins.OmniSharp.Features;

public interface ITargetSpecificOmniSharpServer
{
    Guid ScriptId { get; }
}

public abstract class OmniSharpScriptQuery<TResponse> : Query<TResponse>, ITargetSpecificOmniSharpServer
{
    protected OmniSharpScriptQuery(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}

public abstract class OmniSharpScriptQuery<TInput, TResponse> : OmniSharpScriptQuery<TResponse>
{
    protected OmniSharpScriptQuery(Guid scriptId, TInput input) : base(scriptId)
    {
        Input = input;
    }

    public TInput Input { get; }
}

public abstract class OmniSharpScriptCommand : Command, ITargetSpecificOmniSharpServer
{
    protected OmniSharpScriptCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}

public abstract class OmniSharpScriptCommand<TResponse> : Command<TResponse>, ITargetSpecificOmniSharpServer
{
    protected OmniSharpScriptCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}

public abstract class OmniSharpScriptCommand<TInput, TResponse> : OmniSharpScriptCommand<TResponse>
{
    protected OmniSharpScriptCommand(Guid scriptId, TInput input) : base(scriptId)
    {
        Input = input;
    }

    public TInput Input { get; }
}
