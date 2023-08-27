using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetPad.Assemblies;
using NetPad.Common;
using NetPad.Data.EntityFrameworkCore.DataConnections;

namespace NetPad.Data.EntityFrameworkCore;

public class EntityFrameworkDatabaseConnectionMetadataProvider : IDatabaseConnectionMetadataProvider
{
    private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly IDataConnectionPasswordProtector _dataConnectionPasswordProtector;

    public EntityFrameworkDatabaseConnectionMetadataProvider(
        IDataConnectionResourcesCache dataConnectionResourcesCache,
        IAssemblyLoader assemblyLoader,
        IDataConnectionPasswordProtector dataConnectionPasswordProtector)
    {
        _dataConnectionResourcesCache = dataConnectionResourcesCache;
        _assemblyLoader = assemblyLoader;
        _dataConnectionPasswordProtector = dataConnectionPasswordProtector;
    }

    public async Task<DatabaseStructure> GetDatabaseStructureAsync(DatabaseConnection databaseConnection)
    {
        if (databaseConnection is not EntityFrameworkDatabaseConnection dbConnection)
        {
            throw new ArgumentException("Cannot get structure except on Entity Framework database connections", nameof(databaseConnection));
        }

        await using var dbContext = await CreateDbContextAsync(dbConnection);

        if (dbContext == null)
        {
            return new DatabaseStructure(dbConnection.DatabaseName ?? string.Empty);
        }

        return dbContext.GetDatabaseStructure();
    }

    private async Task<DbContext?> CreateDbContextAsync(EntityFrameworkDatabaseConnection dbConnection)
    {
        var assemblyImage = await _dataConnectionResourcesCache.GetAssemblyAsync(dbConnection, GlobalConsts.AppDotNetFrameworkVersion);

        if (assemblyImage == null)
        {
            return null;
        }

        var assembly = _assemblyLoader.LoadFrom(assemblyImage.Image);

        var dbContextType = assembly.GetExportedTypes().FirstOrDefault(x => typeof(DbContext).IsAssignableFrom(x) && x.BaseType != typeof(DbContext));
        if (dbContextType == null)
        {
            throw new Exception("Could not find a type in data connection assembly of type DbContext.");
        }

        var dbContextOptionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
        var dbContextOptionsBuilder = Activator.CreateInstance(dbContextOptionsBuilderType) as DbContextOptionsBuilder;
        if (dbContextOptionsBuilder == null)
        {
            throw new Exception($"Could not create DbContextOptionsBuilder<> for DbContext of type {dbContextType.FullName}.");
        }

        await dbConnection.ConfigureDbContextOptionsAsync(dbContextOptionsBuilder, _dataConnectionPasswordProtector);

        var ctor = dbContextType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 1);
        if (ctor == null)
        {
            throw new Exception($"Could not create find the right constructor on DbContext of type {dbContextType.FullName}.");
        }

        var dbContext = ctor.Invoke(new object?[] { dbContextOptionsBuilder.Options }) as DbContext;

        if (dbContext == null)
        {
            throw new Exception($"Could not create a DbContext of type {dbContextType.FullName}.");
        }

        return dbContext;
    }
}
