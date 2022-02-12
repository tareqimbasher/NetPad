namespace NetPad.Commands;

public abstract class Command
{
    protected Command()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
}

public abstract class Command<TResponse> : Command
{
}
