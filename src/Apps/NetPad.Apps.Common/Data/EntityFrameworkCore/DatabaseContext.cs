using Microsoft.EntityFrameworkCore;

namespace NetPad.Apps.Data.EntityFrameworkCore;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public static DatabaseContext Create(Action<DbContextOptionsBuilder<DatabaseContext>> configure)
    {
        var dbOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        configure(dbOptionsBuilder);
        return new DatabaseContext(dbOptionsBuilder.Options);
    }
}
