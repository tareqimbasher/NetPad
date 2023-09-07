using MediatR;
using NetPad.Data;
using NetPad.Events;

namespace NetPad.CQs;

public class DeleteDataConnectionCommand : Command
{
    public DeleteDataConnectionCommand(Guid id)
    {
        ConnectionId = id;
    }

    public Guid ConnectionId { get; }

    public class Handler : IRequestHandler<DeleteDataConnectionCommand, Unit>
    {
        private readonly IDataConnectionRepository _dataConnectionRepository;
        private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
        private readonly IEventBus _eventBus;

        public Handler(IDataConnectionRepository dataConnectionRepository, IDataConnectionResourcesCache dataConnectionResourcesCache, IEventBus eventBus)
        {
            _dataConnectionRepository = dataConnectionRepository;
            _dataConnectionResourcesCache = dataConnectionResourcesCache;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(DeleteDataConnectionCommand request, CancellationToken cancellationToken)
        {
            var connection = await _dataConnectionRepository.GetAsync(request.ConnectionId);
            if (connection is null)
                throw new InvalidOperationException($"No data connection with ID '{request.ConnectionId}' was found.");

            await _dataConnectionRepository.DeleteAsync(connection.Id);

            await _dataConnectionResourcesCache.RemoveCachedResourcesAsync(connection.Id);

            await _eventBus.PublishAsync(new DataConnectionDeletedEvent(connection));

            return Unit.Value;
        }
    }
}
