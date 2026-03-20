using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public interface IEntityFrameworkDatabaseConnection
{
    void ConfigureDbContextOptions(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector);
}
