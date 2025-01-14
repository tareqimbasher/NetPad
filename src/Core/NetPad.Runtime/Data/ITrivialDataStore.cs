namespace NetPad.Data;

/// <summary>
/// Represents a key-value data store that persists trivial data. Trivial data is data
/// that if lost does not impact the functionality of the application. Example: Window size/location.
/// </summary>
public interface ITrivialDataStore
{
    TValue? Get<TValue>(string key) where TValue : class;
    void Set<TValue>(string key, TValue value);
    bool Contains(string key);
}
