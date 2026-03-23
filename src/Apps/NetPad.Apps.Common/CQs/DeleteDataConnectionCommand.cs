using MediatR;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Data.Metadata;
using NetPad.Events;

namespace NetPad.Apps.CQs;

public class DeleteDataConnectionCommand(Guid id) : Command
{
    public Guid ConnectionId { get; } = id;

    public class Handler(
        IDataConnectionRepository dataConnectionRepository,
        IDataConnectionResourcesCache dataConnectionResourcesCache,
        IEventBus eventBus)
        : IRequestHandler<DeleteDataConnectionCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteDataConnectionCommand request, CancellationToken cancellationToken)
        {
            var connection = await dataConnectionRepository.GetAsync(request.ConnectionId);
            if (connection is null)
                throw new InvalidOperationException($"No data connection with ID '{request.ConnectionId}' was found.");

            await dataConnectionRepository.DeleteAsync(connection.Id);

            // If server-attached, remove the database from the server's selected databases
            if (connection is DatabaseConnection { ServerId: { } serverId, DatabaseName: { } databaseName })
            {
                var server = await dataConnectionRepository.GetServerAsync(serverId);
                if (server != null && server.SelectedDatabaseNames.Remove(databaseName))
                {
                    await dataConnectionRepository.SaveServerAsync(server);
                    await eventBus.PublishAsync(new DatabaseServerSavedEvent(server));
                }
            }

            await dataConnectionResourcesCache.RemoveCachedResourcesAsync(connection.Id);

            await eventBus.PublishAsync(new DataConnectionDeletedEvent(connection));

            return Unit.Value;
        }
    }
}
