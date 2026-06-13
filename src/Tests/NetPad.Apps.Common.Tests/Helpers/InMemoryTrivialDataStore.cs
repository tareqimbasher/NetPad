using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Helpers;

/// <summary>
/// In-memory <see cref="ITrivialDataStore"/> for tests that need real round-trip storage
/// (unlike the no-op store, which discards writes).
/// </summary>
public class InMemoryTrivialDataStore : ITrivialDataStore
{
    private readonly Dictionary<string, object> _data = new();

    public TValue? Get<TValue>(string key) where TValue : class =>
        _data.TryGetValue(key, out var v) ? (TValue)v : null;

    public void Set<TValue>(string key, TValue value) => _data[key] = value!;

    public bool Contains(string key) => _data.ContainsKey(key);
}
