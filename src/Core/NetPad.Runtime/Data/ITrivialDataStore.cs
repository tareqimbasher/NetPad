namespace NetPad.Data;

/// <summary>
/// Represents a key-value data store that persists trivial data. Trivial data is data we can afford
/// to lose without impacting the functionality of the application. Examples:
///   - Window size/location
///   - The last active script
/// </summary>
public interface ITrivialDataStore
{
    TValue? Get<TValue>(string key) where TValue : class;
    void Set<TValue>(string key, TValue value);
    bool Contains(string key);
}
