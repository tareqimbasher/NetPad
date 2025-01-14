using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NetPad.ExecutionModel.ClientServer.ScriptServices;

/// <summary>
/// A basic memory cache.
/// </summary>
public class MemCache
{
    private readonly ConcurrentDictionary<string, Func<Task<object?>>> _cache = new();

    /// <summary>
    /// Returns whether the given key exists in cache.
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    /// <summary>
    /// Gets a cached value if it exists.
    /// </summary>
    /// <returns>true if cache contains key; false otherwise.</returns>
    public bool TryGet(string key, [NotNullWhen(true)] out Func<Task<object?>>? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets a cached value and throws if key does not exist.
    /// </summary>
    /// <exception cref="KeyNotFoundException">If key is not present in cache.</exception>
    public T? Get<T>(string key)
    {
        if (!_cache.TryGetValue(key, out var factory))
        {
            throw new KeyNotFoundException($"The key '{key}' was not present in cache.");
        }

        var value = factory().Result;
        return (T?)value;
    }

    /// <summary>
    /// Gets value using a cached async factory. If none cached, the provided factory is cached and used to get value.
    /// </summary>
    public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
    {
        var cachedFactory = _cache.GetOrAdd(key, async () => Task.FromResult<object?>(await factory()));
        var value = await cachedFactory();
        return (T?)value;
    }

    /// <summary>
    /// Gets value using a cached factory. If none cached, the provided factory is cached and used to get value.
    /// </summary>
    public T? GetOrAdd<T>(string key, Func<T> factory)
    {
        var cachedFactory = _cache.GetOrAdd(key, () => Task.FromResult<object?>(factory()));
        var value = cachedFactory().Result;
        return (T?)value;
    }

    /// <summary>
    /// Gets cached value. If no value is cached, provided value is cached and returned.
    /// </summary>
    [return: NotNullIfNotNull("value")]
    public T? GetOrAdd<T>(string key, T? value)
    {
        var cachedFactory = _cache.GetOrAdd(key, () => Task.FromResult(value as object));
        var cached = cachedFactory().Result;
        return (T?)cached;
    }

    /// <summary>
    /// Sets the factory to use to get value for the given key.
    /// </summary>
    public void Set<T>(string key, Func<Task<T>> factory)
    {
        _cache[key] = async () => Task.FromResult<object?>(await factory());
    }

    /// <summary>
    /// Sets the factory to use to get value for the given key.
    /// </summary>
    public void Set<T>(string key, Func<T> factory)
    {
        _cache[key] = () => Task.FromResult<object?>(factory());
    }

    /// <summary>
    /// Sets the value to get for the given key.
    /// </summary>
    public void Set<T>(string key, T? value)
    {
        _cache[key] = () => Task.FromResult(value as object);
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
