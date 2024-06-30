using System.Reflection;
using MediatR;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Events;

namespace NetPad.Apps.CQs;

public class SaveDataConnectionCommand(DataConnection connection) : Command
{
    public DataConnection Connection { get; } = connection;

    public class Handler(IDataConnectionRepository dataConnectionRepository, IMediator mediator, IEventBus eventBus)
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
                // We don't want to wait for this
                _ = mediator.Send(new RefreshDataConnectionCommand(updated.Id));
            }

            return Unit.Value;
        }

        private static readonly HashSet<string> _propertiesThatDoNotTriggerResourceRefresh =
        [
            nameof(DataConnection.Name),
            nameof(DatabaseConnection.ContainsProductionData)
        ];

        private bool ShouldRefreshResources(DataConnection existing, DataConnection updated)
        {
            var existingConnectionType = existing.GetType();
            var updatedConnectionType = updated.GetType();

            if (existingConnectionType != updatedConnectionType)
            {
                return true;
            }

            var properties = updatedConnectionType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !_propertiesThatDoNotTriggerResourceRefresh.Contains(p.Name));

            foreach (var property in properties)
            {
                var existingConnectionValue = property.GetValue(existing);
                var updatedConnectionValue = property.GetValue(updated);

                bool changed = !(existingConnectionValue == null && updatedConnectionValue == null) &&
                               (
                                   (existingConnectionValue != null && updatedConnectionValue == null)
                                   || (existingConnectionValue == null && updatedConnectionValue != null)
                                   || existingConnectionValue!.Equals(updatedConnectionValue) != true
                               );

                if (changed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
