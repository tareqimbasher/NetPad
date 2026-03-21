using MediatR;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Events;

namespace NetPad.Apps.CQs;

public class SaveDatabaseServerCommand(DatabaseServerConnection server) : Command
{
    public DatabaseServerConnection Server { get; } = server;

    public class Handler(
        IDataConnectionRepository dataConnectionRepository,
        IMediator mediator,
        IEventBus eventBus)
        : IRequestHandler<SaveDatabaseServerCommand, Unit>
    {
        public async Task<Unit> Handle(SaveDatabaseServerCommand request, CancellationToken cancellationToken)
        {
            var server = request.Server;

            if (server.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Database server connection cannot have a null or empty ID.");
            }

            // Get existing connections attached to this server
            var allConnections = (await dataConnectionRepository.GetAllAsync())
                .OfType<DatabaseConnection>()
                .Where(c => c.ServerId == server.Id)
                .ToList();

            var existingDbNames = allConnections
                .Select(c => c.DatabaseName)
                .Where(n => n != null)
                .Cast<string>()
                .ToHashSet();

            var selectedDbNames = server.SelectedDatabaseNames;

            // Determine which databases were added and removed
            var added = selectedDbNames.Except(existingDbNames).ToList();
            var removed = existingDbNames.Except(selectedDbNames).ToList();

            // Save the server first
            await dataConnectionRepository.SaveServerAsync(server);

            // Delete connections for removed databases
            foreach (var dbName in removed)
            {
                var connection = allConnections.FirstOrDefault(c => c.DatabaseName == dbName);
                if (connection != null)
                {
                    await mediator.Send(new DeleteDataConnectionCommand(connection.Id), cancellationToken);
                }
            }

            // Create connections for newly added databases
            foreach (var dbName in added)
            {
                var connection = server.CreateDatabaseConnection(dbName);
                await mediator.Send(new SaveDataConnectionCommand(connection), cancellationToken);
            }

            await eventBus.PublishAsync(new DatabaseServerSavedEvent(server));

            return Unit.Value;
        }
    }
}
