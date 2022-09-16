using System.Collections.Concurrent;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data;

public class DataConnectionResourcesCache : IDataConnectionResourcesCache
{
    private readonly ConcurrentDictionary<Guid, DataConnectionResources> _cache;
    private readonly IDataConnectionResourcesGenerator _dataConnectionResourcesGenerator;
    private readonly IEventBus _eventBus;

    private readonly object _sourceCodeTaskLock = new object();
    private readonly object _assemblyTaskLock = new object();
    private readonly object _requiredReferencesLock = new object();

    public DataConnectionResourcesCache(IDataConnectionResourcesGenerator dataConnectionResourcesGenerator, IEventBus eventBus)
    {
        _cache = new ConcurrentDictionary<Guid, DataConnectionResources>();
        _dataConnectionResourcesGenerator = dataConnectionResourcesGenerator;
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

            resources ??= _cache.GetOrAdd(dataConnection.Id, new DataConnectionResources(dataConnection));

            resources.SourceCode = Task.Run<SourceCodeCollection>(async () =>
            {
                return await _dataConnectionResourcesGenerator.GenerateSourceCodeAsync(dataConnection);
            });

            resources.SourceCode.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(dataConnection, resources, DataConnectionResourcesUpdatedEvent.UpdatedComponentType.SourceCode));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.SourceCode = null;
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

            resources ??= _cache.GetOrAdd(dataConnection.Id, new DataConnectionResources(dataConnection));

            resources.Assembly = Task.Run<byte[]?>(async () =>
            {
                var sourceCode = await GetSourceGeneratedCodeAsync(dataConnection);
                return await _dataConnectionResourcesGenerator.GenerateAssemblyAsync(dataConnection, sourceCode);
            });

            resources.Assembly.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(dataConnection, resources, DataConnectionResourcesUpdatedEvent.UpdatedComponentType.Assembly));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.Assembly = null;
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

            resources ??= _cache.GetOrAdd(dataConnection.Id, new DataConnectionResources(dataConnection));

            resources.RequiredReferences = Task.Run<IEnumerable<Reference>>(async () =>
            {
                return await _dataConnectionResourcesGenerator.GetRequiredReferencesAsync(dataConnection);
            });

            resources.RequiredReferences.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _eventBus.PublishAsync(new DataConnectionResourcesUpdatedEvent(dataConnection, resources, DataConnectionResourcesUpdatedEvent.UpdatedComponentType.RequiredReferences));
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    // If an error occurred, null the task so the next time its called it tries again
                    resources.RequiredReferences = null;
                }
            });

            return resources.RequiredReferences;
        }
    }
}
