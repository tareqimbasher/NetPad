using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Data;

namespace NetPad.Apps.CQs;

public class RefreshDatabaseServerCommand(Guid serverId) : Command
{
    public Guid ServerId { get; } = serverId;

    public class Handler(
        IDataConnectionRepository dataConnectionRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<Handler> logger)
        : IRequestHandler<RefreshDatabaseServerCommand, Unit>
    {
        public async Task<Unit> Handle(RefreshDatabaseServerCommand request, CancellationToken cancellationToken)
        {
            var server = await dataConnectionRepository.GetServerAsync(request.ServerId);
            if (server is null)
                return Unit.Value;

            var connections = (await dataConnectionRepository.GetAllAsync())
                .OfType<DatabaseConnection>()
                .Where(c => c.ServerId == request.ServerId)
                .ToList();

            if (connections.Count == 0)
            {
                return Unit.Value;
            }

            foreach (var connection in connections)
            {
                RefreshDataConnectionCommand.RunInBackground(connection.Id, serviceScopeFactory, logger);
            }

            return Unit.Value;
        }
    }
}
