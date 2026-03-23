using MediatR;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Events;

namespace NetPad.Apps.CQs;

public class DeleteDatabaseServerCommand(Guid serverId) : Command
{
    public Guid ServerId { get; } = serverId;

    public class Handler(
        IDataConnectionRepository dataConnectionRepository,
        IMediator mediator,
        IEventBus eventBus)
        : IRequestHandler<DeleteDatabaseServerCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteDatabaseServerCommand request, CancellationToken cancellationToken)
        {
            var server = await dataConnectionRepository.GetServerAsync(request.ServerId);
            if (server is null)
                throw new InvalidOperationException($"No database server with ID '{request.ServerId}' was found.");

            // Delete all attached connections first
            var attachedConnections = (await dataConnectionRepository.GetAllAsync())
                .OfType<DatabaseConnection>()
                .Where(c => c.ServerId == request.ServerId)
                .ToList();

            foreach (var connection in attachedConnections)
            {
                await mediator.Send(new DeleteDataConnectionCommand(connection.Id), cancellationToken);
            }

            // Delete the server
            await dataConnectionRepository.DeleteServerAsync(server.Id);

            await eventBus.PublishAsync(new DatabaseServerDeletedEvent(server));

            return Unit.Value;
        }
    }
}
