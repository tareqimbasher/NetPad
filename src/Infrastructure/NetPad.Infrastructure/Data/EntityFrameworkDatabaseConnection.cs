using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data;

public abstract class EntityFrameworkDatabaseConnection : DatabaseConnection
{
    protected EntityFrameworkDatabaseConnection(Guid id, string name, DataConnectionType type, string entityFrameworkProviderName)
        : base(id, name, type)
    {
        EntityFrameworkProviderName = entityFrameworkProviderName;
    }

    public string EntityFrameworkProviderName { get; }

    public abstract Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder);

    public abstract Task<IEnumerable<string>> GetDatabasesAsync();

    public override async Task<DataConnectionTestResult> TestConnectionAsync()
    {
        await using var dbContext = CreateDbContext();

        try
        {
            await dbContext.Database.GetDbConnection().OpenAsync();
            await dbContext.Database.GetDbConnection().CloseAsync();
            return new DataConnectionTestResult(true);
        }
        catch (Exception ex)
        {
            return new DataConnectionTestResult(false, ex.Message);
        }
    }

    public DatabaseContext CreateDbContext()
    {
        return DatabaseContext.Create(options => ConfigureDbContextOptionsAsync(options));
    }
}
