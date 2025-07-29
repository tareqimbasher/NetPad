using Microsoft.EntityFrameworkCore;

namespace NetPad.Apps.Data.EntityFrameworkCore;

/// <summary>
/// A generic database context used by the host to test data connections, get listing of databases...etc.
/// </summary>
public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public static DatabaseContext Create(Action<DbContextOptionsBuilder<DatabaseContext>> configure)
    {
        var dbOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        configure(dbOptionsBuilder);
        return new DatabaseContext(dbOptionsBuilder.Options);
    }
}
