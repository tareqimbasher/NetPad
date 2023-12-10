using NetPad.Data;

namespace NetPad.Tests.Services;

public class NullTrivialDataStore : ITrivialDataStore
{
    public TValue? Get<TValue>(string key) where TValue : class
    {
        return null;
    }

    public void Set<TValue>(string key, TValue value)
    {
    }

    public bool Contains(string key)
    {
        return false;
    }
}
