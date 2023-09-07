using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkSchemaChangeDetectionStrategyBase
{
    protected readonly IDataConnectionResourcesRepository _dataConnectionResourcesRepository;
    protected readonly IDataConnectionPasswordProtector _passwordProtector;

    protected EntityFrameworkSchemaChangeDetectionStrategyBase(
        IDataConnectionResourcesRepository dataConnectionResourcesRepository,
        IDataConnectionPasswordProtector passwordProtector)
    {
        _dataConnectionResourcesRepository = dataConnectionResourcesRepository;
        _passwordProtector = passwordProtector;
    }

    protected async Task ExecuteSqlCommandAsync(EntityFrameworkDatabaseConnection connection, string commandText, Func<DbDataReader, Task> process)
    {
        await using var context = connection.CreateDbContext(_passwordProtector);
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = commandText;
        await context.Database.OpenConnectionAsync();

        await using var result = await command.ExecuteReaderAsync();

        await process(result);
    }
}
