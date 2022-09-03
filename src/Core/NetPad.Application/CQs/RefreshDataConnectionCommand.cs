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
        private readonly IEventBus _eventBus;

        public Handler(IDataConnectionResourcesCache dataConnectionResourcesCache, IEventBus eventBus)
        {
            _dataConnectionResourcesCache = dataConnectionResourcesCache;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(RefreshDataConnectionCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();

            return Unit.Value;
        }
    }
}
