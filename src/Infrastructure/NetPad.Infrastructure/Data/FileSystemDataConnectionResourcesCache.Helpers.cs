using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.DotNet;

namespace NetPad.Data;

public partial class FileSystemDataConnectionResourcesCache
{
    private async Task<DataConnectionResources?> GetCached(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        _logger.LogTrace("Searching cache for data connection {DataConnectionId} resources", dataConnection.Id);

        if (_memoryCache.TryGetValue(dataConnection.Id, out var allFrameworkResources)
            && allFrameworkResources.TryGetValue(dotNetFrameworkVersion, out var memCachedResources))
        {
            _logger.LogTrace("Found data connection {DataConnectionId} resources in memory cache", dataConnection.Id);
            return memCachedResources;
        }

        _logger.LogTrace("Did not find data connection {DataConnectionId} resources in memory cache", dataConnection.Id);

        var diskCachedResources = await Try.RunAsync(async () => await _dataConnectionResourcesRepository.GetAsync(dataConnection, dotNetFrameworkVersion));
        if (diskCachedResources != null)
        {
            _logger.LogTrace("Found data connection {DataConnectionId} resources in disk cache", dataConnection.Id);

            _logger.LogTrace("Checking if data connection {DataConnectionId} schema was modified since {ResourcesRecentAsOf}",
                dataConnection.Id,
                diskCachedResources.RecentAsOf);

            if (await Try.RunAsync(async () => await DidSchemaChangeAsync(dataConnection)) != false)
            {
                await RemoveCachedResourcesAsync(dataConnection.Id);
                return null;
            }

            return UpdateMemCacheAndGetCachedValue(diskCachedResources, dotNetFrameworkVersion);
        }

        _logger.LogTrace("Did not find data connection {DataConnectionId} resources in disk cache", dataConnection.Id);
        return null;
    }

    private async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        foreach (var schemaChangeDetectionStrategy in _dataConnectionSchemaChangeDetectionStrategyFactory.GetStrategies(dataConnection))
        {
            var schemaChanged = await schemaChangeDetectionStrategy.DidSchemaChangeAsync(dataConnection);

            if (schemaChanged == null) continue;

            return schemaChanged.Value;
        }

        return null;
    }

    private async Task<bool> TryUpdateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        var schemaChangeDetectionStrategies = _dataConnectionSchemaChangeDetectionStrategyFactory.GetStrategies(dataConnection);

        bool generatedSchemaCompareInfo = false;

        foreach (var schemaChangeDetectionStrategy in schemaChangeDetectionStrategies)
        {
            var schemaCompareInfo = await schemaChangeDetectionStrategy.GenerateSchemaCompareInfoAsync(dataConnection);

            if (schemaCompareInfo != null)
            {
                generatedSchemaCompareInfo = true;
                await _dataConnectionResourcesRepository.SaveSchemaCompareInfoAsync(dataConnection.Id, schemaCompareInfo);
                break;
            }
        }

        return generatedSchemaCompareInfo;
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
}
