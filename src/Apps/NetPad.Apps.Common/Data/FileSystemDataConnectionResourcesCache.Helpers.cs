using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.DotNet;

namespace NetPad.Apps.Data;

public partial class FileSystemDataConnectionResourcesCache
{
    private async Task<DataConnectionResources?> GetCached(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        logger.LogTrace("Searching cache for data connection {DataConnectionId} resources", dataConnection.Id);

        var memCachedResources = GetResourcesFromMemCache(dataConnection.Id, dotNetFrameworkVersion);
        if (memCachedResources != null)
        {
            return memCachedResources;
        }

        logger.LogTrace("Did not find data connection {DataConnectionId} resources in memory cache", dataConnection.Id);

        var diskCachedResources = await GetResourcesFromRepositoryAsync(dataConnection, dotNetFrameworkVersion);
        if (diskCachedResources != null)
        {
            return diskCachedResources;
        }

        logger.LogTrace("Did not find data connection {DataConnectionId} resources in repository", dataConnection.Id);

        return null;
    }

    private DataConnectionResources? GetResourcesFromMemCache(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        if (_memoryCache.TryGetValue(dataConnectionId, out var allFrameworkResources)
            && allFrameworkResources.TryGetValue(dotNetFrameworkVersion, out var memCachedResources))
        {
            logger.LogTrace("Found data connection {DataConnectionId} resources in memory cache", dataConnectionId);
            return memCachedResources;
        }

        return null;
    }

    private async Task<DataConnectionResources?> GetResourcesFromRepositoryAsync(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        DataConnectionResources? diskCachedResources = null;

        try
        {
            diskCachedResources = await dataConnectionResourcesRepository.GetAsync(dataConnection, dotNetFrameworkVersion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting data connection {DataConnectionId} resources for .NET framework version {DotNetFramework} from repository",
                dataConnection.Id,
                dotNetFrameworkVersion);
        }

        if (diskCachedResources == null)
        {
            return null;
        }

        logger.LogTrace("Found data connection {DataConnectionId} resources in disk cache", dataConnection.Id);

        logger.LogTrace("Checking if data connection {DataConnectionId} schema was modified since {ResourcesRecentAsOf}",
            dataConnection.Id,
            diskCachedResources.RecentAsOf);

        if (await DidSchemaChangeAsync(dataConnection) == false)
        {
            return UpdateMemCacheAndGetCachedValue(diskCachedResources, dotNetFrameworkVersion);
        }

        logger.LogTrace("Found that data connection {DataConnectionId} schema has changed, removing cached resources", dataConnection.Id);
        await RemoveCachedResourcesAsync(dataConnection.Id);
        return null;

    }

    private DataConnectionResources CreateAndMemCacheResources(
        DataConnection dataConnection,
        DotNetFrameworkVersion targetFrameworkVersion,
        DateTime recentAsOf)
    {
        return _memoryCache
            .GetOrAdd(dataConnection.Id, static _ => new ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>())
            .GetOrAdd(targetFrameworkVersion,
                static (_, inputs) => new DataConnectionResources(inputs.DataConnection, inputs.RecentAsOf),
                new CreateResourcesInputs(dataConnection, recentAsOf));
    }

    private DataConnectionResources UpdateMemCacheAndGetCachedValue(DataConnectionResources resources, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return _memoryCache
            .GetOrAdd(resources.DataConnection.Id, static _ => new ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>())
            .AddOrUpdate(targetFrameworkVersion, resources, (_, existing) => existing.UpdateFrom(resources));
    }

    private record CreateResourcesInputs(DataConnection DataConnection, DateTime RecentAsOf);

    private async Task OnResourceGeneratedAsync(DataConnectionResources resources, DotNetFrameworkVersion targetFrameworkVersion, DataConnectionResourceComponent component)
    {
        var dataConnection = resources.DataConnection;

        resources.UpdateRecentAsOf(DateTime.UtcNow);

        try
        {
            if (!await TryUpdateSchemaCompareInfoAsync(dataConnection))
            {
                await dataConnectionResourcesRepository.DeleteSchemaCompareInfoAsync(dataConnection.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error trying to update or delete schema compare info for data connection {DataConnectionId}", dataConnection.Id);
        }

        try
        {
            await dataConnectionResourcesRepository.SaveAsync(resources, targetFrameworkVersion, component);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving {ResourceComponent} resource for data connection {DataConnectionId} for .NET framework version {DotNetFramework} to repository",
                component,
                dataConnection.Id,
                component);
        }

        _ = eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(
            dataConnection,
            resources,
            component));
    }

    private async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        bool strategiesExist = false;

        try
        {
            var strategies = dataConnectionSchemaChangeDetectionStrategyFactory.GetStrategies(dataConnection);

            if (strategies.Any())
            {
                strategiesExist = true;
                _ = eventBus.PublishAsync(new DataConnectionSchemaValidationStartedEvent(dataConnection.Id));
            }

            foreach (var schemaChangeDetectionStrategy in strategies)
            {
                var schemaChanged = await schemaChangeDetectionStrategy.DidSchemaChangeAsync(dataConnection);

                if (schemaChanged == null) continue;

                return schemaChanged.Value;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if data connection {DataConnectionId} schema changed", dataConnection.Id);
            return null;
        }
        finally
        {
            if (strategiesExist)
            {
                _ = eventBus.PublishAsync(new DataConnectionSchemaValidationCompletedEvent(dataConnection.Id));
            }
        }
    }

    private async Task<bool> TryUpdateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        var schemaChangeDetectionStrategies = dataConnectionSchemaChangeDetectionStrategyFactory.GetStrategies(dataConnection);

        bool generatedSchemaCompareInfo = false;

        foreach (var schemaChangeDetectionStrategy in schemaChangeDetectionStrategies)
        {
            var schemaCompareInfo = await schemaChangeDetectionStrategy.GenerateSchemaCompareInfoAsync(dataConnection);

            if (schemaCompareInfo == null) continue;

            generatedSchemaCompareInfo = true;
            await dataConnectionResourcesRepository.SaveSchemaCompareInfoAsync(dataConnection.Id, schemaCompareInfo);
            break;
        }

        return generatedSchemaCompareInfo;
    }
}
