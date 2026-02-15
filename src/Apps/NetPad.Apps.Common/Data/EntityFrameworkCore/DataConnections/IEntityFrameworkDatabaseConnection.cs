using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public interface IEntityFrameworkDatabaseConnection : IDatabaseConnection
{
    void ConfigureDbContextOptions(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector);
}
