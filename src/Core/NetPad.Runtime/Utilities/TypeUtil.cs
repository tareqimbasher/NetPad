using O2Html;

namespace NetPad.Utilities;

public static class TypeUtil
{
    /// <summary>
    /// Gets a readable name for a type (see <see cref="TypeNames.GetReadableName"/>), with its angle brackets escaped
    /// when <paramref name="forHtml"/> is set.
    /// </summary>
    public static string GetReadableName(this Type type, bool withNamespace = false, bool forHtml = false)
    {
        var name = TypeNames.GetReadableName(type, withNamespace);

        return forHtml ? name.Replace("<", "&lt;").Replace(">", "&gt;") : name;
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
