using MediatR;

namespace NetPad.Apps.CQs;

public abstract class CommandBase
{
    public Guid Id { get; } = Guid.NewGuid();
}

public abstract class Command : CommandBase, IRequest;

public abstract class Command<TResponse> : CommandBase, IRequest<TResponse>;

public abstract class QueryBase
{
    public Guid Id { get; } = Guid.NewGuid();
}

public abstract class Query<TResponse> : QueryBase, IRequest<TResponse>;
