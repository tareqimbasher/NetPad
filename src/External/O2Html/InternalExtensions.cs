using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace O2Html;

internal static class InternalExtensions
{
    public static bool In<T>(this T item, params T[] collection)
    {
        return collection.Contains(item);
    }

    public static bool IsDotNetTypeWithStringRepresentation(this Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               typeof(Exception).IsAssignableFrom(type) ||
               type.In(
                   typeof(string),
                   typeof(decimal),
                   typeof(DateTime),
                   typeof(TimeSpan),
                   typeof(DateTimeOffset)
               );
    }


    public static bool IsObjectType(this Type type)
    {
        return !type.IsDotNetTypeWithStringRepresentation() && !type.IsCollectionType();
    }

    public static bool IsCollectionType(this Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    public static IEnumerable<PropertyInfo> GetReadableProperties(this Type type)
    {
        return type.GetProperties().Where(p => p.CanRead);
    }

    public static IEnumerable<PropertyInfo> GetReadableProperties(this object obj)
    {
        return obj.GetType().GetReadableProperties();
    }

    public static string GetReadableName(this Type type, bool withNamespace = false, bool forHtml = false)
    {
        string name = type.FullName ?? type.Name;

        if (type.GenericTypeArguments.Length > 0)
        {
            name = name.Split('`')[0];
            name += "<";
            foreach (var tArg in type.GenericTypeArguments)
            {
                name += GetReadableName(tArg, withNamespace, forHtml) + ", ";
            }

            name = name.TrimEnd(' ', ',') + ">";
        }

        if (!withNamespace)
        {
            string typeNamespace = type.Namespace ?? string.Empty;

            if (typeNamespace.Length > 1 && name.StartsWith(typeNamespace))
                name = name.Substring(typeNamespace.Length + 1); // +1 to trim the '.' after the namespace
        }

        if (forHtml)
            name = name.Replace("<", "&lt;").Replace(">", "&gt;");

        return name;
    }
}
