using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data;

public partial class FileSystemDataConnectionResourcesCache : IDataConnectionResourcesCache
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>> _memoryCache;
    private readonly IDataConnectionResourcesRepository _dataConnectionResourcesRepository;
    private readonly IDataConnectionSchemaChangeDetectionStrategyFactory _dataConnectionSchemaChangeDetectionStrategyFactory;
    private readonly IDataConnectionResourcesGeneratorFactory _dataConnectionResourcesGeneratorFactory;
    private readonly IEventBus _eventBus;
    private readonly ILogger<FileSystemDataConnectionResourcesCache> _logger;

    private readonly SemaphoreSlim _sourceCodeTaskLock = new(1, 1);
    private readonly SemaphoreSlim _assemblyTaskLock = new(1, 1);
    private readonly SemaphoreSlim _requiredReferencesLock = new(1, 1);

    public FileSystemDataConnectionResourcesCache(
        IDataConnectionResourcesRepository dataConnectionResourcesRepository,
        IDataConnectionSchemaChangeDetectionStrategyFactory dataConnectionSchemaChangeDetectionStrategyFactory,
        IDataConnectionResourcesGeneratorFactory dataConnectionResourcesGeneratorFactory,
        IEventBus eventBus,
        ILogger<FileSystemDataConnectionResourcesCache> logger)
    {
        _memoryCache = new();
        _dataConnectionResourcesRepository = dataConnectionResourcesRepository;
        _dataConnectionSchemaChangeDetectionStrategyFactory = dataConnectionSchemaChangeDetectionStrategyFactory;
        _dataConnectionResourcesGeneratorFactory = dataConnectionResourcesGeneratorFactory;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<bool> HasCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        bool hasMemCachedResources = _memoryCache.TryGetValue(dataConnectionId, out var frameworks)
                                     && frameworks.ContainsKey(targetFrameworkVersion);

        return hasMemCachedResources || await _dataConnectionResourcesRepository.HasResourcesAsync(dataConnectionId, targetFrameworkVersion);
    }

    public async Task RemoveCachedResourcesAsync(Guid dataConnectionId)
    {
        await _dataConnectionResourcesRepository.DeleteAsync(dataConnectionId);

        _memoryCache.TryRemove(dataConnectionId, out _);
    }

    public async Task RemoveCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        await _dataConnectionResourcesRepository.DeleteAsync(dataConnectionId, targetFrameworkVersion);

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

            _logger.LogTrace("Generating data connection {DataConnectionId} SourceCode resource", dataConnection.Id);
            _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.SourceCode));

            resources.SourceCode = Task.Run(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateSourceCodeAsync(dataConnection, targetFrameworkVersion);
            });

            _ = resources.SourceCode.ContinueWith(async task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    resources.UpdateRecentAsOf(DateTime.UtcNow);

                    await Try.RunAsync(async () =>
                    {
                        await _dataConnectionResourcesRepository.SaveAsync(resources, targetFrameworkVersion, DataConnectionResourceComponent.SourceCode);

                        _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(
                            dataConnection,
                            resources,
                            DataConnectionResourceComponent.SourceCode));
                    });
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called we try again
                    resources.SourceCode = null;

                    _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
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

            _logger.LogTrace("Generating data connection {DataConnectionId} Assembly resource", dataConnection.Id);
            _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.Assembly));

            resources.Assembly = Task.Run(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateAssemblyAsync(dataConnection, targetFrameworkVersion);
            });

            _ = resources.Assembly.ContinueWith(async task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    resources.UpdateRecentAsOf(DateTime.UtcNow);

                    await Try.RunAsync(async () =>
                    {
                        if (!await TryUpdateSchemaCompareInfoAsync(dataConnection))
                        {
                            await _dataConnectionResourcesRepository.DeleteSchemaCompareInfoAsync(dataConnection.Id);
                        }


                        await _dataConnectionResourcesRepository.SaveAsync(resources, targetFrameworkVersion, DataConnectionResourceComponent.Assembly);
                        _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(
                            dataConnection,
                            resources,
                            DataConnectionResourceComponent.Assembly));
                    });
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called we try again
                    resources.Assembly = null;

                    _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
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

            _logger.LogTrace("Generating data connection {DataConnectionId} RequiredReferences resources", dataConnection.Id);
            _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.RequiredReferences));

            resources.RequiredReferences = Task.Run(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GetRequiredReferencesAsync(dataConnection, targetFrameworkVersion);
            });

            _ = resources.RequiredReferences.ContinueWith(async task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    resources.UpdateRecentAsOf(DateTime.UtcNow);

                    await Try.RunAsync(async () =>
                    {
                        await _dataConnectionResourcesRepository.SaveAsync(
                            resources,
                            targetFrameworkVersion,
                            DataConnectionResourceComponent.RequiredReferences);

                        _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(
                            dataConnection,
                            resources,
                            DataConnectionResourceComponent.RequiredReferences));
                    });
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called we try again
                    resources.RequiredReferences = null;

                    _ = _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
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
