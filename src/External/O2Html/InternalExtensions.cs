using System;
using System.Collections.Generic;
using System.Linq;

namespace O2Html;

internal static class InternalExtensions
{
    public static string ReplaceIfExists(this string source, string strToReplace, string replaceWith)
    {
        if (source.Contains(strToReplace))
            return source.Replace(strToReplace, replaceWith);

        return source;
    }

    public static string GetReadableName(this Type type, bool withNamespace = false)
    {
        string name = type.FullName ?? type.Name;

        if (type.GenericTypeArguments.Length > 0)
        {
            name = name.Split('`')[0];
            name += "<";

            foreach (var tArg in type.GenericTypeArguments)
            {
                name += GetReadableName(tArg, withNamespace) + ", ";
            }

            name = name.TrimEnd(' ', ',') + ">";
        }

        if (!withNamespace)
        {
            string typeNamespace = type.Namespace ?? string.Empty;

            if (typeNamespace.Length > 1 && name.StartsWith(typeNamespace))
                name = name.Substring(typeNamespace.Length + 1); // +1 to trim the '.' after the namespace
        }

        if (type.FullName?.StartsWith("System.Nullable`") == true)
        {
            int iStart = withNamespace ? "System.Nullable<".Length : "Nullable<".Length;
            name = name.Substring(iStart, name.Length - iStart) + "?";
        }

        return name;
    }

    internal static Type? GetCollectionElementType(this Type collectionType)
    {
        // Arrays
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        // IEnumerable<T> collections
        Type? iEnumerable = FindIEnumerable(collectionType);
        if (iEnumerable != null)
        {
            return iEnumerable.GetGenericArguments()[0];
        }

        // Collections that might have an indexer
        var indexerItemType = collectionType.GetProperties().FirstOrDefault(p => p.GetIndexParameters().Length > 0 && p.PropertyType != typeof(object))?.PropertyType;

        return indexerItemType ?? typeof(object);
    }

    private static Type? FindIEnumerable(Type collectionType)
    {
        if (collectionType == typeof(string))
        {
            return null;
        }

        if (collectionType.IsGenericType)
        {
            foreach (Type arg in collectionType.GetGenericArguments())
            {
                Type iEnumerable = typeof(IEnumerable<>).MakeGenericType(arg);
                if (iEnumerable.IsAssignableFrom(collectionType))
                {
                    return iEnumerable;
                }
            }
        }

        Type[] interfaces = collectionType.GetInterfaces();

        foreach (Type iFace in interfaces)
        {
            Type? iEnumerable = FindIEnumerable(iFace);
            if (iEnumerable != null) return iEnumerable;
        }

        if (collectionType.BaseType != null && collectionType.BaseType != typeof(object))
        {
            return FindIEnumerable(collectionType.BaseType);
        }

        return null;
    }
}
