using NetPad.Data;
using NetPad.Events;

namespace NetPad.Apps.Services;

public class RecentScriptsService(ITrivialDataStore trivialDataStore, IEventBus eventBus) : IRecentScriptsService
{
    private const string StorageKey = "session.recentScripts";
    public const int MaxEntries = 10;

    private readonly object _lock = new();

    public IReadOnlyList<string> Get()
    {
        lock (_lock)
        {
            return GetList();
        }
    }

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalized = FileSystemUtil.NormalizePath(path);

        List<string> snapshot;
        lock (_lock)
        {
            var list = GetList();
            list.RemoveAll(p => string.Equals(p, normalized, PlatformUtil.PathComparison));
            list.Insert(0, normalized);

            if (list.Count > MaxEntries)
            {
                list.RemoveRange(MaxEntries, list.Count - MaxEntries);
            }

            trivialDataStore.Set(StorageKey, list);
            snapshot = list;
        }

        _ = eventBus.PublishAsync(new RecentScriptsChangedEvent(snapshot));
    }

    public void Remove(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalized = FileSystemUtil.NormalizePath(path);

        List<string> snapshot;
        lock (_lock)
        {
            var list = GetList();
            var removed = list.RemoveAll(p => string.Equals(p, normalized, PlatformUtil.PathComparison));
            if (removed == 0)
            {
                return;
            }

            trivialDataStore.Set(StorageKey, list);
            snapshot = list;
        }

        _ = eventBus.PublishAsync(new RecentScriptsChangedEvent(snapshot));
    }

    public void Clear()
    {
        lock (_lock)
        {
            trivialDataStore.Set(StorageKey, new List<string>());
        }

        _ = eventBus.PublishAsync(new RecentScriptsChangedEvent([]));
    }

    private List<string> GetList()
    {
        return trivialDataStore.Get<List<string>>(StorageKey) ?? [];
    }
}
