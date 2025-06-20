using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Data.Metadata;
using NetPad.Data.Metadata.ChangeDetection;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Apps.Data;

public sealed partial class FileSystemDataConnectionResourcesCache(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionSchemaChangeDetectionStrategyFactory dataConnectionSchemaChangeDetectionStrategyFactory,
    IDataConnectionResourcesGeneratorFactory dataConnectionResourcesGeneratorFactory,
    IEventBus eventBus,
    ILogger<FileSystemDataConnectionResourcesCache> logger)
    : IDataConnectionResourcesCache, IDisposable
{
    record ResourceGenerationLock(Guid DataConnectionId, DotNetFrameworkVersion TargetFrameworkVersion);

    private readonly ConcurrentDictionary<ResourceGenerationLock, SemaphoreSlim> _resourceGenerationLocks = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>> _memoryCache = new();


    public async Task<bool> HasCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        try
        {
            bool hasMemCachedResources = _memoryCache.TryGetValue(dataConnectionId, out var frameworks)
                                         && frameworks.ContainsKey(targetFrameworkVersion);

            return hasMemCachedResources || await dataConnectionResourcesRepository.HasResourcesAsync(dataConnectionId, targetFrameworkVersion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if data connection {DataConnectionId} has resources for .NET framework version {DotNetFramework}",
                dataConnectionId,
                targetFrameworkVersion);
            return false;
        }
    }

    public async Task<IList<DotNetFrameworkVersion>> GetCachedDotNetFrameworkVersions(Guid dataConnectionId)
    {
        if (_memoryCache.TryGetValue(dataConnectionId, out var frameworks))
        {
            return frameworks.Keys.ToArray();
        }

        return await dataConnectionResourcesRepository.GetCachedDotNetFrameworkVersionsAsync(dataConnectionId);
    }

    public async Task RemoveCachedResourcesAsync(Guid dataConnectionId)
    {
        try
        {
            await dataConnectionResourcesRepository.DeleteAsync(dataConnectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing data connection {DataConnectionId} resources from repository", dataConnectionId);
        }


        _memoryCache.TryRemove(dataConnectionId, out _);
    }

    public async Task RemoveCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        try
        {
            await dataConnectionResourcesRepository.DeleteAsync(dataConnectionId, targetFrameworkVersion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing data connection {DataConnectionId} resources for .NET framework version {DotNetFramework} from repository",
                dataConnectionId,
                targetFrameworkVersion);
        }

        if (_memoryCache.TryGetValue(dataConnectionId, out var frameworks))
        {
            frameworks.TryRemove(targetFrameworkVersion, out _);
        }
    }

    public async Task<DataConnectionResources> GetResourcesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        DataConnectionResources? resources;

        if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null)
        {
            return resources;
        }

        var lckKey = new ResourceGenerationLock(dataConnection.Id, targetFrameworkVersion);
        var lck = _resourceGenerationLocks.GetOrAdd(lckKey, static _ => new SemaphoreSlim(1, 1));

        await lck.WaitAsync();

        try
        {
            // Double check after acquiring lock
            if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null)
            {
                return resources;
            }

            logger.LogTrace("Generating data connection resources for: {DataConnectionId}", dataConnection.Id);
            _ = eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, targetFrameworkVersion));

            var generator = dataConnectionResourcesGeneratorFactory.Create(dataConnection);

            resources = await generator.GenerateResourcesAsync(dataConnection, targetFrameworkVersion);

            await OnResourceGeneratedAsync(resources, targetFrameworkVersion);

            UpdateMemCacheAndGetCachedValue(resources, targetFrameworkVersion);

            return resources;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating data connection resources for: {DataConnectionId}", dataConnection.Id);
            _ = eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(dataConnection, targetFrameworkVersion, ex));
            throw;
        }
        finally
        {
            lck.Release();
        }
    }

    public void Dispose()
    {
        foreach (var lockKey in _resourceGenerationLocks.Keys)
        {
            if (_resourceGenerationLocks.TryRemove(lockKey, out var semaphore))
            {
                semaphore.Dispose();
            }
        }
    }
}
