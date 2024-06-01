using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Apps.Data;

public partial class FileSystemDataConnectionResourcesCache(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionSchemaChangeDetectionStrategyFactory dataConnectionSchemaChangeDetectionStrategyFactory,
    IDataConnectionResourcesGeneratorFactory dataConnectionResourcesGeneratorFactory,
    IEventBus eventBus,
    ILogger<FileSystemDataConnectionResourcesCache> logger)
    : IDataConnectionResourcesCache
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>> _memoryCache = new();

    private readonly SemaphoreSlim _sourceCodeTaskLock = new(1, 1);
    private readonly SemaphoreSlim _assemblyTaskLock = new(1, 1);
    private readonly SemaphoreSlim _requiredReferencesLock = new(1, 1);

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

    public async Task<DataConnectionSourceCode> GetSourceGeneratedCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        DataConnectionResources? resources;

        if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null && resources.SourceCode != null)
        {
            return await resources.SourceCode;
        }

        await _sourceCodeTaskLock.WaitAsync();

        try
        {
            // Double check after acquiring lock
            if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null && resources.SourceCode != null)
            {
                return await resources.SourceCode;
            }

            resources ??= CreateAndMemCacheResources(dataConnection, targetFrameworkVersion, DateTime.UtcNow);

            logger.LogTrace("Generating data connection {DataConnectionId} SourceCode resource", dataConnection.Id);
            _ = eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.SourceCode));

            resources.SourceCode = Task.Run(async () =>
            {
                var generator = dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateSourceCodeAsync(dataConnection, targetFrameworkVersion);
            });

            _ = resources.SourceCode.ContinueWith(async task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    await OnResourceGeneratedAsync(resources, targetFrameworkVersion, DataConnectionResourceComponent.SourceCode);
                }
                else if (task.Status is TaskStatus.Faulted or TaskStatus.Canceled)
                {
                    // If an error occurred, null the task so the next time its called we try again
                    resources.SourceCode = null;

                    _ = eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
                        dataConnection,
                        DataConnectionResourceComponent.SourceCode,
                        task.Exception));
                }
            });
        }
        finally
        {
            _sourceCodeTaskLock.Release();
        }

        return await resources.SourceCode;
    }

    public async Task<AssemblyImage?> GetAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        DataConnectionResources? resources;

        if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null && resources.Assembly != null)
        {
            return await resources.Assembly;
        }

        await _assemblyTaskLock.WaitAsync();

        try
        {
            // Double check after acquiring lock
            if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null && resources.Assembly != null)
            {
                return await resources.Assembly;
            }

            resources ??= CreateAndMemCacheResources(dataConnection, targetFrameworkVersion, DateTime.UtcNow);

            logger.LogTrace("Generating data connection {DataConnectionId} Assembly resource", dataConnection.Id);
            _ = eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.Assembly));

            resources.Assembly = Task.Run(async () =>
            {
                var generator = dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateAssemblyAsync(dataConnection, targetFrameworkVersion);
            });

            _ = resources.Assembly.ContinueWith(async task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    await OnResourceGeneratedAsync(resources, targetFrameworkVersion, DataConnectionResourceComponent.Assembly);
                }
                else if (task.Status is TaskStatus.Faulted or TaskStatus.Canceled)
                {
                    // If an error occurred, null the task so the next time its called we try again
                    resources.Assembly = null;

                    _ = eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
                        dataConnection,
                        DataConnectionResourceComponent.Assembly,
                        task.Exception));
                }
            });
        }
        finally
        {
            _assemblyTaskLock.Release();
        }

        return await resources.Assembly;
    }

    public async Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        DataConnectionResources? resources;

        if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null && resources.RequiredReferences != null)
        {
            return await resources.RequiredReferences;
        }

        await _requiredReferencesLock.WaitAsync();

        try
        {
            // Double check after acquiring lock
            if ((resources = await GetCached(dataConnection, targetFrameworkVersion)) != null && resources.RequiredReferences != null)
            {
                return await resources.RequiredReferences;
            }

            resources ??= CreateAndMemCacheResources(dataConnection, targetFrameworkVersion, DateTime.UtcNow);

            logger.LogTrace("Generating data connection {DataConnectionId} RequiredReferences resources", dataConnection.Id);
            _ = eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.RequiredReferences));

            resources.RequiredReferences = Task.Run(async () =>
            {
                var generator = dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GetRequiredReferencesAsync(dataConnection, targetFrameworkVersion);
            });

            _ = resources.RequiredReferences.ContinueWith(async task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    await OnResourceGeneratedAsync(resources, targetFrameworkVersion, DataConnectionResourceComponent.RequiredReferences);
                }
                else if (task.Status is TaskStatus.Faulted or TaskStatus.Canceled)
                {
                    // If an error occurred, null the task so the next time its called we try again
                    resources.RequiredReferences = null;

                    _ = eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
                        dataConnection,
                        DataConnectionResourceComponent.RequiredReferences,
                        task.Exception));
                }
            });
        }
        finally
        {
            _requiredReferencesLock.Release();
        }

        return await resources.RequiredReferences;
    }
}
