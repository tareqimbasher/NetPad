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
        private readonly IEventBus _eventBus;

        public Handler(IDataConnectionRepository dataConnectionRepository, IEventBus eventBus)
        {
            _dataConnectionRepository = dataConnectionRepository;
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(SaveDataConnectionCommand request, CancellationToken cancellationToken)
        {
            await _dataConnectionRepository.SaveAsync(request.Connection);

            await _eventBus.PublishAsync(new DataConnectionSavedEvent(request.Connection));

            return Unit.Value;
        }
    }
}
