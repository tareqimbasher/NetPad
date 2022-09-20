using MediatR;
using NetPad.Data;
using NetPad.Events;

namespace NetPad.CQs;

public class RefreshDataConnectionCommand : Command
{
    public RefreshDataConnectionCommand(Guid id)
    {
        ConnectionId = id;
    }

    public Guid ConnectionId { get; }

    public class Handler : IRequestHandler<RefreshDataConnectionCommand, Unit>
    {
        private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
        private readonly IDataConnectionRepository _dataConnectionRepository;
        private readonly IEventBus _eventBus;

        public Handler(IDataConnectionResourcesCache dataConnectionResourcesCache, IDataConnectionRepository dataConnectionRepository, IEventBus eventBus)
        {
            _dataConnectionResourcesCache = dataConnectionResourcesCache;
            _dataConnectionRepository = dataConnectionRepository;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(RefreshDataConnectionCommand request, CancellationToken cancellationToken)
        {
            var connection = await _dataConnectionRepository.GetAsync(request.ConnectionId);

            if (connection == null)
            {
                return Unit.Value;
            }

            _dataConnectionResourcesCache.RemoveCachedResources(request.ConnectionId);

            await _dataConnectionResourcesCache.GetAssemblyAsync(connection);

            return Unit.Value;
        }
    }
}
