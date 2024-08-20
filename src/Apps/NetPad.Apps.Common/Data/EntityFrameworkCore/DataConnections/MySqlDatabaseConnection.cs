using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class MySqlDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    private readonly PomeloDatabaseConnection _pomeloDatabaseConnection;

    public MySqlDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
        : base(id, name, DataConnectionType.MySQL, "Pomelo.EntityFrameworkCore.MySql", scaffoldOptions)
    {
        _pomeloDatabaseConnection = new(() => (
            Host, 
            Port, 
            DatabaseName, 
            UserId, 
            Password, 
            ConnectionStringAugment));
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector) =>
        _pomeloDatabaseConnection.GetConnectionString(passwordProtector);

    public override async Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector) =>
        await _pomeloDatabaseConnection.ConfigureDbContextOptionsAsync(builder, passwordProtector);

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using DatabaseContext context = CreateDbContext(passwordProtector);

        return await _pomeloDatabaseConnection.GetDatabasesAsync(passwordProtector, context);
    }
}