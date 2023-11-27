namespace NetPad.Data;

/// <summary>
/// Represents a key-value data store.
/// </summary>
public interface IKeyValueDataStore
{
    TValue? Get<TValue>(string key) where TValue : class;
    void Set<TValue>(string key, TValue value);
    bool Contains(string key);
}
