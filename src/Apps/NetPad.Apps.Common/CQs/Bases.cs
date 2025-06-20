using MediatR;

namespace NetPad.Apps.CQs;

public abstract class CommandBase
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// A command to take an action or write data with no return data.
/// </summary>
public abstract class Command : CommandBase, IRequest;

/// <summary>
/// A command to take an action or write data that also returns data.
/// </summary>
/// <typeparam name="TResponse">The type of data this command returns.</typeparam>
public abstract class Command<TResponse> : CommandBase, IRequest<TResponse>;

public abstract class QueryBase
{
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// A query to read or get data.
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public abstract class Query<TResponse> : QueryBase, IRequest<TResponse>;
