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

            _ = Task.Run(async () =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var tasks = connections.Select(async connection =>
                {
                    try
                    {
                        await mediator.Send(new RefreshDataConnectionCommand(connection.Id));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Error refreshing resources for connection {ConnectionId} on server {ServerId}",
                            connection.Id, request.ServerId);
                    }
                });

                await Task.WhenAll(tasks);
            });

            return Unit.Value;
        }
    }
}
