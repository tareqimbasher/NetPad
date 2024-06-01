namespace NetPad.Utilities;

public static class TypeUtil
{
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
                name = name[(typeNamespace.Length + 1)..]; // +1 to trim the '.' after the namespace
        }

        if (forHtml)
        {
            name = name.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        if (type.FullName?.StartsWith("System.Nullable`") == true)
        {
            int iStart = withNamespace ? "System.Nullable<".Length : "Nullable<".Length;
            name = name[iStart..^1] + "?";
        }

        return name;
    }

    public static bool IsOfGenericType(this Type typeToCheck, Type genericType)
    {
        Type? type = typeToCheck;

        while (type != null)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }
}
