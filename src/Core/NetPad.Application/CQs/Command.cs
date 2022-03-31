using MediatR;

namespace NetPad.CQs;

public abstract class CommandBase
{
    protected CommandBase()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
}

public abstract class Command : CommandBase, IRequest
{
}

public abstract class Command<TResponse> : CommandBase, IRequest<TResponse>
{
}

public abstract class QueryBase
{
    protected QueryBase()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
}

public abstract class Query<TResponse> : QueryBase, IRequest<TResponse>
{
}
