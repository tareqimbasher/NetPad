using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Sessions;

namespace NetPad.Apps.Data.EntityFrameworkCore;

internal class EntityFrameworkDatabaseConnectionMetadataProvider(IDataConnectionResourcesCache dataConnectionResourcesCache, ISession session)
    : IDatabaseConnectionMetadataProvider
{
    public async Task<DatabaseStructure> GetDatabaseStructureAsync(DatabaseConnection databaseConnection)
    {
        if (databaseConnection is not EntityFrameworkDatabaseConnection dbConnection)
        {
            throw new ArgumentException("Cannot get structure except on Entity Framework database connections", nameof(databaseConnection));
        }

        DotNetFrameworkVersion target;

        // If we have something scaffolded already, use that.
        var available = await dataConnectionResourcesCache.GetCachedDotNetFrameworkVersions(databaseConnection.Id);
        if (available.Count > 0)
        {
            target = available.Max();
        }
        // If we are going to scaffold to get structure, prioritize the active script's framework version.
        // This will generate resources for the user's currently opened script which the user will likely run after they've inspected the structure.
        else if (session.Active != null)
        {
            target = session.Active.Script.Config.TargetFrameworkVersion;
        }
        else
        {
            target = GlobalConsts.AppDotNetFrameworkVersion;
        }

        var resources = await dataConnectionResourcesCache.GetResourcesAsync(dbConnection, target);

        return resources.DatabaseStructure ??
               throw new Exception($"Could not get database structure during scaffolding. Check the logs at: {AppDataProvider.LogDirectoryPath}");
    }
}
