using System.Reflection;
using MediatR;
using NetPad.Data;
using NetPad.Events;

namespace NetPad.CQs;

public class SaveDataConnectionCommand : Command
{
    public SaveDataConnectionCommand(DataConnection connection)
    {
        Connection = connection;
    }

    public DataConnection Connection { get; }

    public class Handler : IRequestHandler<SaveDataConnectionCommand, Unit>
    {
        private readonly IDataConnectionRepository _dataConnectionRepository;
        private readonly IMediator _mediator;
        private readonly IEventBus _eventBus;

        public Handler(IDataConnectionRepository dataConnectionRepository, IMediator mediator, IEventBus eventBus)
        {
            _dataConnectionRepository = dataConnectionRepository;
            _mediator = mediator;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(SaveDataConnectionCommand request, CancellationToken cancellationToken)
        {
            var updated = request.Connection;
            var existing = await _dataConnectionRepository.GetAsync(request.Connection.Id);

            await _dataConnectionRepository.SaveAsync(updated);

            await _eventBus.PublishAsync(new DataConnectionSavedEvent(updated));

            bool shouldRefreshResources = existing == null || HasChangedOtherThanName(existing, updated);

            if (shouldRefreshResources)
            {
                // We don't want to wait for this
                _ = _mediator.Send(new RefreshDataConnectionCommand(updated.Id));
            }

            return Unit.Value;
        }

        private bool HasChangedOtherThanName(DataConnection existing, DataConnection updated)
        {
            var existingConnectionType = existing.GetType();
            var updatedConnectionType = updated.GetType();

            if (existingConnectionType != updatedConnectionType)
            {
                return true;
            }

            var properties = updatedConnectionType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.Name != nameof(DataConnection.Name));

            foreach (var property in properties)
            {
                var existingConnectionValue = property.GetValue(existing);
                var updatedConnectionValue = property.GetValue(updated);

                bool changed = (existingConnectionValue != null && updatedConnectionValue == null)
                               || (existingConnectionValue == null && updatedConnectionValue != null)
                               || existingConnectionValue?.Equals(updatedConnectionValue) != true;

                if (changed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
