namespace NetPad.Data;

/// <summary>
/// Represents a key-value data store that stores trivial data. Trivial data is data
/// that is not essential to the functionality of the application or the user experience.
/// </summary>
public interface ITrivialDataStore
{
    TValue? Get<TValue>(string key) where TValue : class;
    void Set<TValue>(string key, TValue value);
    bool Contains(string key);
}
