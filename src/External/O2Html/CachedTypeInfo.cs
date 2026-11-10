using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace O2Html;

/// <summary>
/// What <see cref="HtmlSerializer"/> has worked out about one type. Each value is computed on first use.
/// </summary>
internal sealed class CachedTypeInfo
{
    private const int Unset = -1;

    private int _typeCategory = Unset;
    private int _valueKind = Unset;
    private PropertyInfo[]? _readableProperties;
    private StrongBox<Type?>? _collectionElementType;

    public bool TryGetTypeCategory(out TypeCategory category)
    {
        var value = Volatile.Read(ref _typeCategory);
        category = (TypeCategory)value;
        return value != Unset;
    }

    public void SetTypeCategory(TypeCategory category) => Volatile.Write(ref _typeCategory, (int)category);

    public bool TryGetValueKind(out ValueKind kind)
    {
        var value = Volatile.Read(ref _valueKind);
        kind = (ValueKind)value;
        return value != Unset;
    }

    public void SetValueKind(ValueKind kind) => Volatile.Write(ref _valueKind, (int)kind);

    /// <summary>
    /// The type's readable properties, or <see langword="null" /> when not computed yet.
    /// </summary>
    public PropertyInfo[]? ReadableProperties
    {
        get => Volatile.Read(ref _readableProperties);
        set => Volatile.Write(ref _readableProperties, value);
    }

    /// <summary>
    /// The type's collection element type, or <see langword="null" /> when not computed yet. A box that holds
    /// <see langword="null" /> means it was computed and the type has no element type.
    /// </summary>
    public StrongBox<Type?>? CollectionElementType
    {
        get => Volatile.Read(ref _collectionElementType);
        set => Volatile.Write(ref _collectionElementType, value);
    }
}
