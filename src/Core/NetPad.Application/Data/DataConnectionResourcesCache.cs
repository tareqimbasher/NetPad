using System.Collections.Concurrent;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data;

public class DataConnectionResourcesCache : IDataConnectionResourcesCache
{
    private readonly ConcurrentDictionary<Guid, DataConnectionResources> _cache;
    private readonly IDataConnectionResourcesGeneratorFactory _dataConnectionResourcesGeneratorFactory;
    private readonly IEventBus _eventBus;

    private readonly object _sourceCodeTaskLock = new object();
    private readonly object _assemblyTaskLock = new object();
    private readonly object _requiredReferencesLock = new object();

    public DataConnectionResourcesCache(IDataConnectionResourcesGeneratorFactory dataConnectionResourcesGeneratorFactory, IEventBus eventBus)
    {
        _cache = new ConcurrentDictionary<Guid, DataConnectionResources>();
        _dataConnectionResourcesGeneratorFactory = dataConnectionResourcesGeneratorFactory;
        _eventBus = eventBus;
    }

    public bool HasCachedResources(Guid dataConnectionId)
    {
        return _cache.ContainsKey(dataConnectionId);
    }

    public void RemoveCachedResources(Guid dataConnectionId)
    {
        _cache.Remove(dataConnectionId, out _);
    }

    public Task<SourceCodeCollection> GetSourceGeneratedCodeAsync(DataConnection dataConnection)
    {
        if (_cache.TryGetValue(dataConnection.Id, out var resources) && resources.SourceCode != null)
        {
            return resources.SourceCode;
        }

        lock (_sourceCodeTaskLock)
        {
            // Double check after acquiring lock
            if (_cache.TryGetValue(dataConnection.Id, out resources) && resources.SourceCode != null)
            {
                return resources.SourceCode;
            }

            resources ??= CreateResources(dataConnection);

            _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.SourceCode));

            resources.SourceCode = Task.Run<SourceCodeCollection>(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateSourceCodeAsync(dataConnection);
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
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(dataConnection, DataConnectionResourceComponent.SourceCode, task.Exception));
                }
            });

            return resources.SourceCode;
        }
    }

    public Task<byte[]?> GetAssemblyAsync(DataConnection dataConnection)
    {
        if (_cache.TryGetValue(dataConnection.Id, out var resources) && resources.Assembly != null)
        {
            return resources.Assembly;
        }

        lock (_assemblyTaskLock)
        {
            // Double check after acquiring lock
            if (_cache.TryGetValue(dataConnection.Id, out resources) && resources.Assembly != null)
            {
                return resources.Assembly;
            }

            resources ??= CreateResources(dataConnection);

            _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.Assembly));

            resources.Assembly = Task.Run<byte[]?>(async () =>
            {
                var sourceCode = await GetSourceGeneratedCodeAsync(dataConnection);

                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GenerateAssemblyAsync(dataConnection, sourceCode);
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
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(dataConnection, DataConnectionResourceComponent.Assembly, task.Exception));
                }
            });

            return resources.Assembly;
        }
    }

    public Task<IEnumerable<Reference>> GetRequiredReferencesAsync(DataConnection dataConnection)
    {
        if (_cache.TryGetValue(dataConnection.Id, out var resources) && resources.RequiredReferences != null)
        {
            return resources.RequiredReferences;
        }

        lock (_requiredReferencesLock)
        {
            // Double check after acquiring lock
            if (_cache.TryGetValue(dataConnection.Id, out resources) && resources.RequiredReferences != null)
            {
                return resources.RequiredReferences;
            }

            resources ??= CreateResources(dataConnection);

            _eventBus.PublishAsync(new DataConnectionResourcesUpdatingEvent(dataConnection, DataConnectionResourceComponent.RequiredReferences));

            resources.RequiredReferences = Task.Run<IEnumerable<Reference>>(async () =>
            {
                var generator = _dataConnectionResourcesGeneratorFactory.Create(dataConnection);
                return await generator.GetRequiredReferencesAsync(dataConnection);
            });

            resources.RequiredReferences.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(dataConnection, resources, DataConnectionResourceComponent.RequiredReferences));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.RequiredReferences = null;
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdateFailedEvent(dataConnection, DataConnectionResourceComponent.RequiredReferences, task.Exception));
                }
            });

            return resources.RequiredReferences;
        }
    }

    private DataConnectionResources CreateResources(DataConnection dataConnection)
    {
        return _cache.GetOrAdd(dataConnection.Id, static (key, dc) => new DataConnectionResources(dc), dataConnection);
    }
}
