using System;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data.EntityFrameworkCore;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public static DatabaseContext Create(Action<DbContextOptionsBuilder<DatabaseContext>> configure)
    {
        var dbOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        configure(dbOptionsBuilder);
        return new DatabaseContext(dbOptionsBuilder.Options);
    }
}
