using MediatR;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
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

            // Check if scaffold options changed compared to existing server
            var existingServer = await dataConnectionRepository.GetServerAsync(server.Id);
            bool scaffoldOptionsChanged = DidScaffoldOptionsChange(existingServer, server);

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

            // If scaffold options changed, refresh existing connections that weren't added or removed
            if (scaffoldOptionsChanged)
            {
                var retained = allConnections
                    .Where(c => c.DatabaseName != null && !added.Contains(c.DatabaseName) && !removed.Contains(c.DatabaseName));

                foreach (var connection in retained)
                {
                    _ = mediator.Send(new RefreshDataConnectionCommand(connection.Id));
                }
            }

            await eventBus.PublishAsync(new DatabaseServerSavedEvent(server));

            return Unit.Value;
        }

        private static bool DidScaffoldOptionsChange(DatabaseServerConnection? existing, DatabaseServerConnection updated)
        {
            var existingOptions = (existing as EntityFrameworkDatabaseServerConnection)?.ScaffoldOptions;
            var updatedOptions = (updated as EntityFrameworkDatabaseServerConnection)?.ScaffoldOptions;

            return existingOptions != updatedOptions;
        }
    }
}
