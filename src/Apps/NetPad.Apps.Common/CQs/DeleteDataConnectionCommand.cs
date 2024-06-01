using MediatR;
using NetPad.Data;
using NetPad.Data.Events;
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

            await dataConnectionResourcesCache.RemoveCachedResourcesAsync(connection.Id);

            await eventBus.PublishAsync(new DataConnectionDeletedEvent(connection));

            return Unit.Value;
        }
    }
}
