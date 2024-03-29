using System;
using System.Collections.Generic;

namespace O2Html;

public class SerializationScope
{
    private readonly HashSet<object> _serializedObjects;

    public SerializationScope(int depth)
    {
        Depth = depth;
        _serializedObjects  = new HashSet<object>(Common.ReferenceEqualityComparer.Instance);
    }

    public SerializationScope(int depth, SerializationScope parentScope)
    {
        Depth = depth;
        _serializedObjects  = new HashSet<object>(
            parentScope.SerializedObjects ?? throw new ArgumentNullException(nameof(parentScope)),
            Common.ReferenceEqualityComparer.Instance
        );
    }

    public int Depth { get; }

    public IReadOnlyCollection<object> SerializedObjects => _serializedObjects;

    public bool CheckAlreadySerializedOrAdd<T>(T obj)
    {
        if (obj == null)
            return false;

        bool alreadySerialized = _serializedObjects.Contains(obj);

        if (!alreadySerialized)
        {
            _serializedObjects.Add(obj);
        }

        return alreadySerialized;
    }
}
