using System.Collections.Concurrent;

namespace NetPad.ExecutionModel.ScriptServices;

/// <summary>
/// Info about an item stored in MemCache.
/// </summary>
/// <param name="Key">The unique key that identifies this item in cache.</param>
/// <param name="ValueType">The name of the CLR type of the value.</param>
/// <param name="ValueInitialized">
/// Whether the value has been initialized yet. If a factory was used when adding the cache item, this will return
/// whether that factory was executed or not. If an object or value was used when adding the cache item, this
/// will always return true.
/// </param>
/// <param name="IsFactory">Whether a factory, or an async factory, was used when adding the item to the cache.</param>
public record MemCacheItemInfo(string Key, string ValueType, bool ValueInitialized, bool IsFactory);

/// <summary>
/// A basic memory cache.
/// </summary>
public class MemCache
{
    public event EventHandler<EventArgs>? MemCacheItemInfoChanged;

    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    private class CacheItem(Type valueType)
    {
        public string ValueType { get; } = valueType.GetReadableName();
        public bool IsInstanceSet { get; private set; }
        public object? Instance { get; private set; }
        public Lazy<object?>? Factory { get; private set; }

        public CacheItem WithInstance(object? instance)
        {
            Instance = instance;
            IsInstanceSet = true;
            return this;
        }

        public CacheItem WithFactory(Lazy<object?> factory)
        {
            Factory = factory;
            return this;
        }

        public object? GetValue()
        {
            return Factory != null ? Factory.Value : Instance;
        }
    }

    /// <summary>
    /// The keys this cache contains.
    /// </summary>
    public ICollection<string> Keys => _cache.Keys;

    /// <summary>
    /// Determines whether the cache contains the specified key.
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key from the cache. Throws if the key does not exist.
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
    /// Retrieves the value associated with the specified key from the cache. Throws if the key does not exist.
    /// </summary>
    /// <exception cref="KeyNotFoundException">If key is not present in cache.</exception>
    public object? Get(string key)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            throw new KeyNotFoundException($"The key '{key}' was not present in cache.");
        }

        return cached.GetValue();
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the cache if it exists.
    /// </summary>
    /// <returns>true if key was found; otherwise, false.</returns>
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
    /// Attempts to retrieve the value associated with the specified key from the cache if it exists.
    /// </summary>
    /// <returns>true if cache contains key; false otherwise.</returns>
    public bool TryGet(string key, out object? value)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            value = null;
            return false;
        }

        value = cached.GetValue();
        return true;
    }

    /// <summary>
    /// Retrieves the value associated with the specified key from the cache. If the key does not exist,
    /// the specified value is added to the cache and returned.
    /// </summary>
    public T? GetOrAdd<T>(string key, T? value)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            cached = _cache.GetOrAdd(key, new CacheItem(typeof(T)).WithInstance(value));
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        return (T?)cached.Instance;
    }

    /// <summary>
    /// Retrieves the value associated with the specified key from the cache. If the key does not exist,
    /// the specified factory function is used to generate the value, which is then cached and returned.
    /// </summary>
    public T? GetOrAdd<T>(string key, Func<T> factory)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            cached = _cache.GetOrAdd(key, new CacheItem(typeof(T)).WithFactory(new Lazy<object?>(() => factory())));
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        return (T?)cached.GetValue();
    }

    /// <summary>
    /// Retrieves the value associated with the specified key from the cache. If the key does not exist,
    /// the specified factory function is used to generate the value, which is then cached and returned.
    /// Note that if the async factory is executed, it will block the calling thread until completion.
    /// </summary>
    public T? GetOrAdd<T>(string key, Func<Task<T>> factory)
    {
        if (!_cache.TryGetValue(key, out var cached))
        {
            cached = _cache.GetOrAdd(key,
                new CacheItem(typeof(T)).WithFactory(new Lazy<object?>(() => factory().Result)));
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        return (T?)cached.GetValue();
    }

    /// <summary>
    /// Sets the value for the specified key. If the key does not exist, it is added to the cache.
    /// </summary>
    public void Set<T>(string key, T? value)
    {
        _cache[key] = new CacheItem(typeof(T)).WithInstance(value);
        MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the factory function for the specified key. If the key does not exist, it is added to the cache.
    /// The factory function will be executed the first time the key is requested.
    /// </summary>
    public void Set<T>(string key, Func<T> factory)
    {
        _cache[key] = new CacheItem(typeof(T)).WithFactory(new Lazy<object?>(() => factory()));
        MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the asynchronous factory function for the specified key. If the key does not exist, it is added
    /// to the cache. The factory function will be executed the first time the key is requested, blocking the
    /// calling thread until completion.
    /// </summary>
    public void Set<T>(string key, Func<Task<T>> factory)
    {
        _cache[key] = new CacheItem(typeof(T)).WithFactory(new Lazy<object?>(() => factory().Result));
        MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Attempts to add the specified key and value to the cache. If the key already exists, no action is taken.
    /// </summary>
    public void TryAdd<T>(string key, T? value)
    {
        if (_cache.TryAdd(key, new CacheItem(typeof(T)).WithInstance(value)))
        {
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Attempts to add the specified key and factory function to the cache. If the key already exists, no
    /// action is taken.
    /// </summary>
    public void TryAdd<T>(string key, Func<T> factory)
    {
        if (_cache.TryAdd(key, new CacheItem(typeof(T)).WithFactory(new Lazy<object?>(() => factory()))))
        {
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Attempts to add the specified key and asynchronous factory function to the cache. If the key already
    /// exists, no action is taken.
    /// </summary>
    public void TryAdd<T>(string key, Func<Task<T>> factory)
    {
        if (_cache.TryAdd(key, new CacheItem(typeof(T)).WithFactory(new Lazy<object?>(() => factory().Result))))
        {
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Removes the cache entry associated with the specified key, if it exists.
    /// </summary>
    public void Remove(string key)
    {
        if (_cache.TryRemove(key, out _))
        {
            MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Removes all keys and values from the cache.
    /// </summary>
    public void Clear()
    {
        if (_cache.IsEmpty)
        {
            return;
        }

        _cache.Clear();
        MemCacheItemInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns metadata about the keys and values currently in cache.
    /// </summary>
    public MemCacheItemInfo[] GetItemInfos()
    {
        return _cache.Select(x => new MemCacheItemInfo(
                x.Key,
                x.Value.ValueType,
                x.Value.Factory is { IsValueCreated: true } || x.Value.IsInstanceSet,
                x.Value.Factory != null
            ))
            .ToArray();
    }
}
