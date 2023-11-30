#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using O2Html.Dom;

namespace O2Html.Converters;

public class MemoryHtmlConverter : CollectionHtmlConverter
{
    private const string ToArrayMethodName = "ToArray";
    private static readonly HashSet<Type> _canConvertTypes = new()
    {
        typeof(Memory<>),
        typeof(ReadOnlyMemory<>),
    };

    public override bool CanConvert(Type type)
    {
        return type.IsGenericType && _canConvertTypes.Contains(type.GetGenericTypeDefinition());
    }

    protected override (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        var method = type.GetMethod(ToArrayMethodName, BindingFlags.Public | BindingFlags.Instance);

        if (method == null)
        {
            throw new HtmlSerializationException($"Cannot serialize type {type}. {ToArrayMethodName} method not found.");
        }

        Array array = (Array)method.Invoke(obj, Array.Empty<object>());
        return base.Convert(array, array.GetType(), serializationScope, htmlSerializer);
    }
}
#endif
