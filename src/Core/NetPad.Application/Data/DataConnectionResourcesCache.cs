using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data;

public class DataConnectionResourcesCache : IDataConnectionResourcesCache
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>> _cache;
    private readonly IDataConnectionResourcesGeneratorFactory _dataConnectionResourcesGeneratorFactory;
    private readonly IEventBus _eventBus;

    private readonly object _sourceCodeTaskLock = new();
    private readonly object _assemblyTaskLock = new();
    private readonly object _requiredReferencesLock = new();

    public DataConnectionResourcesCache(IDataConnectionResourcesGeneratorFactory dataConnectionResourcesGeneratorFactory, IEventBus eventBus)
    {
        _cache = new();
        _dataConnectionResourcesGeneratorFactory = dataConnectionResourcesGeneratorFactory;
        _eventBus = eventBus;
    }

    private bool TryGetCached(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion, [NotNullWhen(true)] out DataConnectionResources? resources)
    {
        if (_cache.TryGetValue(dataConnectionId, out var allFrameworkResources)
            && allFrameworkResources.TryGetValue(targetFrameworkVersion, out var targetFrameworkResources))
        {
            resources = targetFrameworkResources;
            return true;
        }

        resources = null;
        return false;
    }

    public Dictionary<DotNetFrameworkVersion, DataConnectionResources>? GetCached(Guid dataConnectionId)
    {
        return _cache.TryGetValue(dataConnectionId, out var allFrameworkResources)
            ? allFrameworkResources.ToDictionary(kv => kv.Key, kv => kv.Value)
            : null;
    }

    public bool HasCachedResources(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return TryGetCached(dataConnectionId, targetFrameworkVersion, out _);
    }

    public void RemoveCachedResources(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (HasCachedResources(dataConnectionId, targetFrameworkVersion) && _cache.Remove(dataConnectionId, out var frameworks))
        {
            frameworks.Remove(targetFrameworkVersion, out _);
        }
    }

    public Task<DataConnectionSourceCode> GetSourceGeneratedCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (TryGetCached(dataConnection.Id, targetFrameworkVersion, out var resources) && resources.SourceCode != null)
        {
            return resources.SourceCode;
        }

        lock (_sourceCodeTaskLock)
        {
            // Double check after acquiring lock
            if (TryGetCached(dataConnection.Id, targetFrameworkVersion, out resources) && resources.SourceCode != null)
            {
                return resources.SourceCode;
            }

            resources ??= CreateResources(dataConnection, targetFrameworkVersion);

            _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.SourceCode));

            resources.SourceCode = Task.Run(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateSourceCodeAsync(dataConnection, targetFrameworkVersion);
            });

            resources.SourceCode.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(dataConnection, resources, DataConnectionResourceComponent.SourceCode));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.SourceCode = null;
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
                        dataConnection,
                        DataConnectionResourceComponent.SourceCode,
                        task.Exception));
                }
            });

            return resources.SourceCode;
        }
    }

    public Task<AssemblyImage?> GetAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (TryGetCached(dataConnection.Id, targetFrameworkVersion, out var resources) && resources.Assembly != null)
        {
            return resources.Assembly;
        }

        lock (_assemblyTaskLock)
        {
            // Double check after acquiring lock
            if (TryGetCached(dataConnection.Id, targetFrameworkVersion, out resources) && resources.Assembly != null)
            {
                return resources.Assembly;
            }

            resources ??= CreateResources(dataConnection, targetFrameworkVersion);

            _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.Assembly));

            resources.Assembly = Task.Run(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateAssemblyAsync(dataConnection, targetFrameworkVersion);
            });

            resources.Assembly.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(dataConnection, resources, DataConnectionResourceComponent.Assembly));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.Assembly = null;
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
                        dataConnection,
                        DataConnectionResourceComponent.Assembly,
                        task.Exception));
                }
            });

            return resources.Assembly;
        }
    }

    public Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (TryGetCached(dataConnection.Id, targetFrameworkVersion, out var resources) && resources.RequiredReferences != null)
        {
            return resources.RequiredReferences;
        }

        lock (_requiredReferencesLock)
        {
            // Double check after acquiring lock
            if (TryGetCached(dataConnection.Id, targetFrameworkVersion, out resources) && resources.RequiredReferences != null)
            {
                return resources.RequiredReferences;
            }

            resources ??= CreateResources(dataConnection, targetFrameworkVersion);

            _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.RequiredReferences));

            resources.RequiredReferences = Task.Run(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GetRequiredReferencesAsync(dataConnection, targetFrameworkVersion);
            });

            resources.RequiredReferences.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(
                        dataConnection,
                        resources,
                        DataConnectionResourceComponent.RequiredReferences));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.RequiredReferences = null;
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(
                        dataConnection,
                        DataConnectionResourceComponent.RequiredReferences,
                        task.Exception));
                }
            });

            return resources.RequiredReferences;
        }
    }

    private DataConnectionResources CreateResources(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return _cache
            .GetOrAdd(dataConnection.Id, static (key) => new ConcurrentDictionary<DotNetFrameworkVersion, DataConnectionResources>())
            .GetOrAdd(targetFrameworkVersion, static (key, dc) => new DataConnectionResources(dc), dataConnection);
    }
}
