using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Events;

namespace NetPad.Apps.CQs;

public class SaveDataConnectionCommand(DataConnection connection) : Command
{
    public DataConnection Connection { get; } = connection;

    public class Handler(
        IDataConnectionRepository dataConnectionRepository,
        IServiceScopeFactory serviceScopeFactory,
        IEventBus eventBus,
        ILogger<Handler> logger)
        : IRequestHandler<SaveDataConnectionCommand, Unit>
    {
        public async Task<Unit> Handle(SaveDataConnectionCommand request, CancellationToken cancellationToken)
        {
            if (request.Connection.Id == default)
            {
                throw new InvalidOperationException("Data connection cannot have a null or empty ID");
            }

            var updated = request.Connection;
            var existing = await dataConnectionRepository.GetAsync(request.Connection.Id);

            await dataConnectionRepository.SaveAsync(updated);

            await eventBus.PublishAsync(new DataConnectionSavedEvent(updated));

            bool shouldRefreshResources = existing == null || ShouldRefreshResources(existing, updated);

            if (shouldRefreshResources)
            {
                RefreshDataConnectionCommand.RunInBackground(updated.Id, serviceScopeFactory, logger);
            }

            return Unit.Value;
        }

        private static readonly HashSet<string> _propertiesThatDoNotTriggerResourceRefresh =
        [
            nameof(DataConnection.Name),
            nameof(DatabaseConnection.ContainsProductionData),
            nameof(DatabaseConnection.Server),
        ];

        private static bool ShouldRefreshResources(DataConnection existing, DataConnection updated)
        {
            if (existing.GetType() != updated.GetType())
            {
                return true;
            }

            return PropertyChangeDetector.HasChanges(existing, updated, _propertiesThatDoNotTriggerResourceRefresh);
        }
    }
}
