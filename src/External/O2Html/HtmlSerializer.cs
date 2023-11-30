using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using O2Html.Converters;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html;

public sealed class HtmlSerializer
{
    private static readonly HtmlSerializerSettings _htmlSerializerSettings = new();
    private static readonly ConcurrentDictionary<Type, HtmlConverter?> _typeConverterCache = new();
    private static readonly ConcurrentDictionary<Type, TypeCategory> _typeCategoryCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _typePropertyCache = new();
    private static readonly ConcurrentDictionary<Type, Type?> _collectionElementTypeCache = new();

    public HtmlSerializer(HtmlSerializerSettings? serializerSettings = null)
    {
        SerializerSettings = serializerSettings ?? _htmlSerializerSettings;
        Converters = new List<HtmlConverter>();

        // Insert settings converters at the beginning so they take precedence
        Converters.AddRange(SerializerSettings.Converters);

        // Add default converters in this order, first converter in list
        // that can convert object takes precedence
        Converters.Add(new FileSystemInfoHtmlConverter());
        Converters.Add(new TwoDimensionalArrayHtmlConverter());
        Converters.Add(new DataSetHtmlConverter());
        Converters.Add(new DataTableHtmlConverter());
        Converters.Add(new XNodeHtmlConverter());
        Converters.Add(new XmlNodeHtmlConverter());
        Converters.Add(new DotNetTypeWithStringRepresentationHtmlConverter());
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        Converters.Add(new TupleHtmlConverter());
#endif
#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
        Converters.Add(new MemoryHtmlConverter());
#endif
        Converters.Add(new CollectionHtmlConverter());
        Converters.Add(new ObjectHtmlConverter());
    }

    public HtmlSerializerSettings SerializerSettings { get; }

    public List<HtmlConverter> Converters { get; }

    public Node Serialize<T>(T? obj, Type type, SerializationScope? serializationScope = null)
    {
        if (obj == null)
        {
            return new Null(SerializerSettings.CssClasses.Null);
        }

        var converter = GetConverter(type);
        if (converter == null)
            throw new HtmlSerializationException($"Could not find a {nameof(HtmlConverter)} for type: {type}");

        var isSimpleType = GetTypeCategory(type) == TypeCategory.DotNetTypeWithStringRepresentation;

        if ((!isSimpleType && serializationScope?.Depth > SerializerSettings.MaxDepth) ||
            // Let's serialize values with string representations at the last depth level
            (isSimpleType && serializationScope?.Depth > SerializerSettings.MaxDepth + 1))
        {
            return new MaxDepthReached(SerializerSettings.CssClasses.MaxDepthReached);
        }

        serializationScope = GetSerializationScope(type, obj, serializationScope);

        return converter.WriteHtml(obj, type, serializationScope, this);
    }

    public void SerializeWithinTableRow<T>(Element tr, T? obj, Type type, SerializationScope? serializationScope = null)
    {
        var converter = GetConverter(type);
        if (converter == null)
            throw new HtmlSerializationException($"Could not find a {nameof(HtmlConverter)} for type: {type}");

        if (serializationScope?.Depth > SerializerSettings.MaxDepth)
        {
            return;
        }

        serializationScope = GetSerializationScope(type, obj, serializationScope);

        converter.WriteHtmlWithinTableRow(tr, obj, type, serializationScope, this);
    }

    internal TypeCategory GetTypeCategory(Type type)
    {
        if (_typeCategoryCache.TryGetValue(type, out var category))
            return category;

        if (IsDotNetTypeWithStringRepresentation(type))
            category = TypeCategory.DotNetTypeWithStringRepresentation;
        else if (IsCollectionType(type))
            category = TypeCategory.Collection;
        else
            category = TypeCategory.SingleObject;

        _typeCategoryCache.TryAdd(type, category);
        return category;
    }

    internal PropertyInfo[] GetReadableProperties(Type type)
    {
        if (_typePropertyCache.TryGetValue(type, out var propertyInfos))
            return propertyInfos;

        propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            // Exclude properties that exist in base types and are hidden by properties in derived types
            .GroupBy(p => p.Name)
            .Select(g => g.OrderBy(p => p.DeclaringType == type).First())
            .ToArray();

        _typePropertyCache.TryAdd(type, propertyInfos);
        return propertyInfos;
    }

    public Type? GetCollectionElementType(Type collectionType)
    {
        if (_collectionElementTypeCache.TryGetValue(collectionType, out var elementType))
            return elementType;

        elementType = collectionType.GetCollectionElementType();

        _collectionElementTypeCache.TryAdd(collectionType, elementType);

        return elementType;
    }

    internal HtmlConverter? GetConverter(Type type)
    {
        if (_typeConverterCache.TryGetValue(type, out var match))
            return match;

        foreach (var converter in Converters)
        {
            if (!converter.CanConvert(this, type)) continue;
            _typeConverterCache.TryAdd(type, converter);
            return converter;
        }

        _typeConverterCache.TryAdd(type, null);
        return null;
    }

    private SerializationScope GetSerializationScope<T>(Type type, T? obj, SerializationScope? serializationScope)
    {
        return serializationScope == null
            ? new SerializationScope(0)
            : new SerializationScope(serializationScope.Depth + 1, serializationScope);
    }

    public static bool IsDotNetTypeWithStringRepresentation(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || typeof(IFormattable).IsAssignableFrom(type)
               || typeof(Exception).IsAssignableFrom(type)
               || typeof(Type).IsAssignableFrom(type)
               || Nullable.GetUnderlyingType(type) != null
               || typeof(XNode).IsAssignableFrom(type)
               || typeof(XmlNode).IsAssignableFrom(type)
            ;
    }

    public static bool IsObjectType(Type type)
    {
        return !IsDotNetTypeWithStringRepresentation(type) && !IsCollectionType(type);
    }

    public static bool IsCollectionType(Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    public static HtmlSerializer Create(HtmlSerializerSettings? serializerSettings = null)
    {
        return new HtmlSerializer(serializerSettings);
    }
}
