using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NetPad.ExecutionModel.ClientServer.ScriptServices;

/// <summary>
/// A basic memory cache.
/// </summary>
public class MemCache
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    private class CacheItem
    {
        public object? Instance { get; set; }
        public Lazy<object?>? Factory { get; set; }

        public object? GetValue()
        {
            return Factory != null ? Factory.Value : Instance;
        }
    }

    /// <summary>
    /// Returns whether the given key exists in cache.
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    /// <summary>
    /// Gets a cached value and throws if key does not exist.
    /// </summary>
    /// <exception cref="KeyNotFoundException">If key is not present in cache.</exception>
    public T? Get<T>(string key)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            throw new KeyNotFoundException($"The key '{key}' was not present in cache.");
        }

        return (T?)cached.GetValue();
    }

    /// <summary>
    /// Gets a cached value if it exists.
    /// </summary>
    /// <returns>true if cache contains key; false otherwise.</returns>
    public bool TryGet<T>(string key, out T? value)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            value = default;
            return false;
        }

        value = (T?)cached.GetValue();
        return true;
    }

    /// <summary>
    /// Gets cached value. If no value is cached, provided value is cached and returned.
    /// </summary>
    [return: NotNullIfNotNull("value")]
    public T? GetOrAdd<T>(string key, T? value)
    {
        var cached = _cache.GetOrAdd(key, new CacheItem { Instance = value });
        return (T?)cached.Instance;
    }

    /// <summary>
    /// Gets value using a cached factory. If none cached, the provided factory is cached and used to get value.
    /// </summary>
    public T? GetOrAdd<T>(string key, Func<T> factory)
    {
        var cached = _cache.GetOrAdd(key, new CacheItem { Factory = new Lazy<object?>(() => factory()) });
        return (T?)cached.GetValue();
    }

    /// <summary>
    /// Gets value using a cached async factory. If none cached, the provided factory is cached and used to get value.
    /// </summary>
    public T? GetOrAdd<T>(string key, Func<Task<T>> factory)
    {
        var cached = _cache.GetOrAdd(key, new CacheItem { Factory = new Lazy<object?>(() => factory().Result) });
        return (T?)cached.GetValue();
    }

    /// <summary>
    /// Sets the value to get for the given key.
    /// </summary>
    public void Set<T>(string key, T? value)
    {
        _cache[key] = new CacheItem { Instance = value };
    }

    /// <summary>
    /// Sets the factory to use to get value for the given key.
    /// </summary>
    public void Set<T>(string key, Func<T> factory)
    {
        _cache[key] = new CacheItem { Factory = new Lazy<object?>(() => factory()) };
    }

    /// <summary>
    /// Sets the factory to use to get value for the given key.
    /// </summary>
    public void Set<T>(string key, Func<Task<T>> factory)
    {
        _cache[key] = new CacheItem { Factory = new Lazy<object?>(() => factory().Result) };
    }

    /// <summary>
    /// Removes a cache entry if it exists.
    /// </summary>
    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Removes all cached entries.
    /// </summary>
    public void Clear() => _cache.Clear();
}
