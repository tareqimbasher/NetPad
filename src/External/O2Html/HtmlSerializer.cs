using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using O2Html.Converters;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html;

/// <summary>
/// Provides methods for converting .NET objects or value types to HTML.
/// </summary>
public sealed class HtmlSerializer
{

    // Using a weak table because a strong static reference to a Type from a collectible load context, like a
    // user script's types, would keep that context, and every assembly in it, loaded.
    private static readonly ConditionalWeakTable<Type, CachedTypeInfo> _typeInfoCache = new();

    // Per instance, because each serializer has its own converters.
    private readonly ConcurrentDictionary<Type, HtmlConverter?> _converterCache = new();

    private static readonly HashSet<Type> _numericTypes = new()
    {
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
        typeof(IntPtr), typeof(UIntPtr),
    };

    private static readonly HashSet<Type> _temporalTypes = new()
    {
        typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
#if NET6_0_OR_GREATER
        typeof(DateOnly), typeof(TimeOnly),
#endif
    };

    // First converter in list that can convert type will be selected.
    private static readonly HtmlConverter[] _defaultHtmlConverters = new HtmlConverter[]
    {
        new FileSystemInfoHtmlConverter(),
        new TwoDimensionalArrayHtmlConverter(),
        new DataSetHtmlConverter(),
        new DataTableHtmlConverter(),
#if NETCOREAPP3_0_OR_GREATER
        new JsonDocumentHtmlConverter(),
#endif
        new XNodeHtmlConverter(),
        new XmlNodeHtmlConverter(),
        new DotNetTypeWithStringRepresentationHtmlConverter(),
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        new TupleHtmlConverter(),
#endif
#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
        new MemoryHtmlConverter(),
#endif
        new CollectionHtmlConverter(),
        new ObjectHtmlConverter(),
    };

    public HtmlSerializer(HtmlSerializerOptions? serializerSettings = null)
    {
        SerializerOptions = serializerSettings ?? new HtmlSerializerOptions();

        // First converter in list that can convert type will be selected.
        Converters = new List<HtmlConverter>();

        // Insert user-defined converters at the beginning so they take precedence.
        Converters.AddRange(SerializerOptions.Converters);

        // Add default converters after user-defined ones.
        Converters.AddRange(_defaultHtmlConverters);
    }

    public HtmlSerializerOptions SerializerOptions { get; }

    public List<HtmlConverter> Converters { get; }


    /// <summary>
    /// Serializes an object to a HTML DOM object tree.
    /// </summary>
    /// <param name="obj">The object or value type to serialize.</param>
    /// <param name="htmlSerializerSettings">Serialization settings.</param>
    /// <typeparam name="T">Type of object being serialized.</typeparam>
    /// <returns>HTML DOM object tree representing the object or value type being serialized.</returns>
    public static Node Serialize<T>(T? obj, HtmlSerializerOptions? htmlSerializerSettings = null)
    {
        var serializer = Create(htmlSerializerSettings);

        var type = typeof(T);

        if (obj != null && type == typeof(object))
        {
            type = obj.GetType();
        }

        return serializer.Serialize(obj, type);
    }

    public Node Serialize<T>(T? obj, Type type, SerializationScope? serializationScope = null)
    {
        if (obj == null)
        {
            return new Null(SerializerOptions.CssClasses.Null);
        }

        serializationScope = GetSerializationScope(serializationScope);

        if (ShouldShortCircuit(obj, type, serializationScope, SerializerOptions, out var shortCircuitValue))
        {
            return shortCircuitValue!;
        }

        var converter = GetConverter(type);
        if (converter == null)
        {
            throw new HtmlSerializationException($"Could not find a {nameof(HtmlConverter)} for type: {type}");
        }

        return converter.WriteHtml(obj, type, serializationScope, this);
    }

    public void SerializeWithinTableRow<T>(TableRow tr, T? obj, Type type, SerializationScope? serializationScope = null)
    {
        if (obj == null)
        {
            this.AddAndGetValueCell(tr, obj)
                .AddAndGetNull().AddClass(SerializerOptions.CssClasses.Null);

            return;
        }

        var converter = GetConverter(type);
        if (converter == null)
            throw new HtmlSerializationException($"Could not find a {nameof(HtmlConverter)} for type: {type}");

        serializationScope = GetSerializationScope(serializationScope);

        if (serializationScope.Depth > SerializerOptions.MaxDepth)
        {
            return;
        }

        converter.WriteHtmlWithinTableRow(tr, obj, type, serializationScope, this);
    }

    internal static TypeCategory GetTypeCategory(Type type)
    {
        var cached = _typeInfoCache.GetOrCreateValue(type);
        if (cached.TryGetTypeCategory(out var category))
            return category;

        if (IsDotNetTypeWithStringRepresentation(type))
        {
            category = TypeCategory.DotNetTypeWithStringRepresentation;
        }
        else if (IsCollectionType(type))
        {
            category = TypeCategory.Collection;
        }
        else
        {
            category = TypeCategory.SingleObject;
        }

        cached.SetTypeCategory(category);
        return category;
    }

    internal static ValueKind GetValueKind(Type type)
    {
        var cached = _typeInfoCache.GetOrCreateValue(type);
        if (cached.TryGetValueKind(out var kind))
        {
            return kind;
        }

        var valueType = Nullable.GetUnderlyingType(type) ?? type;

        if (valueType.IsEnum)
        {
            kind = ValueKind.Enum;
        }
        else if (valueType == typeof(bool))
        {
            kind = ValueKind.Boolean;
        }
        else if (_numericTypes.Contains(valueType))
        {
            kind = ValueKind.Numeric;
        }
        else if (_temporalTypes.Contains(valueType))
        {
            kind = ValueKind.Temporal;
        }
        else
        {
            kind = ValueKind.Text;
        }

        cached.SetValueKind(kind);
        return kind;
    }

    internal HtmlConverter? GetConverter(Type type)
    {
        if (_converterCache.TryGetValue(type, out var match))
            return match;

        foreach (var converter in Converters)
        {
            if (!converter.CanConvert(type)) continue;
            _converterCache.TryAdd(type, converter);
            return converter;
        }

        _converterCache.TryAdd(type, null);
        return null;
    }

    private SerializationScope GetSerializationScope(SerializationScope? serializationScope)
    {
        return serializationScope == null
            ? new SerializationScope(0)
            : new SerializationScope(serializationScope.Depth + 1, serializationScope);
    }


    private static bool ShouldShortCircuit<T>(
        T obj,
        Type type,
        SerializationScope serializationScope,
        HtmlSerializerOptions htmlSerializerOptions,
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
#endif
        out Node? shortCircuitValue)
    {
        shortCircuitValue = null;

        // Check #1: Check Max Depth
        var isSimpleType = GetTypeCategory(type) == TypeCategory.DotNetTypeWithStringRepresentation;

        if ((!isSimpleType && serializationScope.Depth > htmlSerializerOptions.MaxDepth) ||
            // Let's serialize values with string representations at the last depth level
            (isSimpleType && serializationScope.Depth > htmlSerializerOptions.MaxDepth + 1))
        {
            shortCircuitValue = new MaxDepthReached(htmlSerializerOptions.CssClasses.MaxDepthReached);
            return true;
        }

        // Check #2: Check if we have a reference loop
        if (!serializationScope.CheckAlreadySerializedOrAdd(obj))
        {
            return false;
        }

        var referenceLoopHandling = htmlSerializerOptions.ReferenceLoopHandling;

        if (referenceLoopHandling == ReferenceLoopHandling.IgnoreAndSerializeCyclicReference)
        {
            shortCircuitValue = new CyclicReference(type).AddClass(htmlSerializerOptions.CssClasses.CyclicReference);
        }
        else if (referenceLoopHandling == ReferenceLoopHandling.Ignore)
        {
            shortCircuitValue = new Element("div");
        }
        else if (referenceLoopHandling == ReferenceLoopHandling.Error)
        {
            throw new HtmlSerializationException($"A reference loop was detected. Object already serialized: {type.FullName}");
        }

        return shortCircuitValue != null;
    }

    internal static PropertyInfo[] GetReadableProperties(Type type)
    {
        var cached = _typeInfoCache.GetOrCreateValue(type);
        var propertyInfos = cached.ReadableProperties;
        if (propertyInfos != null)
            return propertyInfos;

        propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Only include readable properties, and exclude indexer properties
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            // Exclude properties that exist in base types and are hidden by properties in derived types
            .GroupBy(p => p.Name)
            .Select(g => g.OrderBy(p => p.DeclaringType == type).First())
            .ToArray();

        cached.ReadableProperties = propertyInfos;
        return propertyInfos;
    }

    public static Type? GetCollectionElementType(Type collectionType)
    {
        var cached = _typeInfoCache.GetOrCreateValue(collectionType);
        var elementTypeBox = cached.CollectionElementType;
        if (elementTypeBox != null)
            return elementTypeBox.Value;

        var elementType = collectionType.GetCollectionElementType();

        cached.CollectionElementType = new StrongBox<Type?>(elementType);

        return elementType;
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

    public static HtmlSerializer Create(HtmlSerializerOptions? serializerSettings = null)
    {
        return new HtmlSerializer(serializerSettings);
    }
}
