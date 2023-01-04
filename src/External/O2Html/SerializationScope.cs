using System;
using System.Collections.Generic;

namespace O2Html;

public class SerializationScope
{
    private readonly HashSet<object> _serializedObjects;

    public SerializationScope()
    {
        _serializedObjects  = new HashSet<object>();
    }

    public SerializationScope(SerializationScope parentScope)
    {
        _serializedObjects  = new HashSet<object>(parentScope.SerializedObjects
                                                  ?? throw new ArgumentNullException(nameof(parentScope)));
    }

    public IReadOnlyCollection<object> SerializedObjects => _serializedObjects;

    public bool CheckAddAddIsAlreadySerialized<T>(T obj)
    {
        if (obj == null)
            return false;

        bool alreadySerialized = _serializedObjects.Contains(obj);
        if (!alreadySerialized)
            _serializedObjects.Add(obj);
        return alreadySerialized;
    }
}
