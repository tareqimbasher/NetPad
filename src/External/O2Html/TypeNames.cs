using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace O2Html;

/// <summary>
/// Names types for display in C#'s syntax, e.g. <c>List&lt;Int32&gt;</c> instead of the runtime's <c>List`1</c>.
/// </summary>
public static class TypeNames
{
    private const string AnonymousTypeName = "AnonymousType";

    // Ordered from most to least specific: a lookup is also a sequence of groups, and an ordered or queryable
    // sequence is also a plain one.
    private static readonly Type[] _sequenceInterfaces =
    {
        typeof(IGrouping<,>), typeof(ILookup<,>), typeof(IOrderedEnumerable<>), typeof(IQueryable<>), typeof(IEnumerable<>),
#if !NETSTANDARD2_0
        typeof(IAsyncEnumerable<>),
#endif
    };

    private static readonly ConcurrentDictionary<Type, string> _names = new();
    private static readonly ConcurrentDictionary<Type, string> _namesWithNamespace = new();

    /// <summary>
    /// Gets a readable name for a type, e.g. <c>Dictionary&lt;String, Int32?&gt;[]</c>.
    /// </summary>
    /// <remarks>
    /// Without the namespace, a sequence class the user can't name (one the compiler generated, or a framework
    /// class that isn't public, like the iterator <c>Where</c> returns, or LINQ's group and lookup classes, public in
    /// older runtimes) is named after the public interface it stands for, e.g. <c>IEnumerable&lt;Order&gt;</c>,
    /// because those class names change between .NET versions. With the namespace, the name keeps the real class.
    /// </remarks>
    /// <param name="type">The type to name.</param>
    /// <param name="withNamespace">
    /// When <see langword="true" />, the name includes the namespace and, for nested types, the declaring types.
    /// </param>
    public static string GetReadableName(Type type, bool withNamespace = false)
    {
        return withNamespace
            ? _namesWithNamespace.GetOrAdd(type, static t => BuildName(t, withNamespace: true))
            : _names.GetOrAdd(type, static t => BuildName(t, withNamespace: false));
    }

    private static string BuildName(Type type, bool withNamespace)
    {
        if (type.IsArray)
        {
            return BuildArrayName(type, withNamespace);
        }

        if (type.IsGenericParameter)
        {
            // .NET reports a generic parameter, like the T in List<T>, as nested in List<T>, so the nested-type path
            // below would name it List<T>.T.
            return type.Name;
        }

        var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
        if (nullableUnderlyingType != null)
        {
            return GetReadableName(nullableUnderlyingType, withNamespace) + "?";
        }

        if (IsAnonymousType(type))
        {
            var sb = new StringBuilder(AnonymousTypeName);
            var typeArgs = type.GetGenericArguments();
            AppendGenericArguments(sb, typeArgs, 0, typeArgs.Length, withNamespace);
            return sb.ToString();
        }

        if (!withNamespace)
        {
            var sequenceInterface = FindSequenceInterfaceItStandsFor(type);
            if (sequenceInterface != null)
            {
                return GetReadableName(sequenceInterface, withNamespace: false);
            }
        }

        if (!type.IsGenericType && !type.IsNested)
        {
            return withNamespace ? type.FullName ?? type.Name : type.Name;
        }

        var builder = new StringBuilder();
        AppendNestedOrGenericName(builder, type, type.GetGenericArguments(), withNamespace);
        return builder.ToString();
    }

    // C# writes the outermost array's rank first: int[][,] is an array of int[,]. The runtime's names put it last.
    private static string BuildArrayName(Type type, bool withNamespace)
    {
        var rankSuffixes = new StringBuilder();
        var elementType = type;

        while (elementType.IsArray)
        {
            rankSuffixes.Append('[').Append(',', elementType.GetArrayRank() - 1).Append(']');
            elementType = elementType.GetElementType()!;
        }

        return GetReadableName(elementType, withNamespace) + rankSuffixes;
    }

    // typeArgs are the innermost type's generic arguments, which start with those it inherits from its declaring types,
    // outermost first. Each type in the chain takes the next ones. They can't come from the declaring types themselves:
    // Type.DeclaringType returns the generic type definition, e.g. Dictionary<TKey, TValue>.
    private static void AppendNestedOrGenericName(StringBuilder sb, Type type, Type[] typeArgs, bool withNamespace)
    {
        var declaringType = type.DeclaringType;
        int inheritedArgCount = declaringType?.GetGenericArguments().Length ?? 0;
        int argCount = type.GetGenericArguments().Length;

        if (declaringType != null && (withNamespace || inheritedArgCount > 0))
        {
            AppendNestedOrGenericName(sb, declaringType, typeArgs, withNamespace);
            sb.Append('.');
        }
        else if (withNamespace && type.Namespace != null)
        {
            sb.Append(type.Namespace).Append('.');
        }

        var name = type.Name;
        int backtick = name.IndexOf('`');
        sb.Append(name, 0, backtick < 0 ? name.Length : backtick);

        if (argCount > inheritedArgCount)
        {
            AppendGenericArguments(sb, typeArgs, inheritedArgCount, argCount, withNamespace);
        }
    }

    private static void AppendGenericArguments(StringBuilder sb, Type[] typeArgs, int start, int end, bool withNamespace)
    {
        sb.Append('<');
        for (int i = start; i < end; i++)
        {
            if (i > start) sb.Append(", ");
            sb.Append(GetReadableName(typeArgs[i], withNamespace));
        }

        sb.Append('>');
    }

    private static bool IsAnonymousType(Type type) =>
        type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal)
        && type.IsDefined(typeof(CompilerGeneratedAttribute), false);

    private static Type? FindSequenceInterfaceItStandsFor(Type type)
    {
        if (!IsHiddenFromTheUser(type))
        {
            return null;
        }

        var interfaces = type.GetInterfaces();

        foreach (var sequenceInterface in _sequenceInterfaces)
        {
            foreach (var iFace in interfaces)
            {
                if (iFace.IsGenericType && iFace.GetGenericTypeDefinition() == sequenceInterface)
                {
                    return iFace;
                }
            }
        }

        return typeof(IEnumerable).IsAssignableFrom(type) ? typeof(IEnumerable) : null;
    }

    private static bool IsHiddenFromTheUser(Type type)
    {
        if (type.Name.IndexOf('<') >= 0 || type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return true;
        }

        // The user's own non-public classes are names they wrote, so only the framework's are replaced.
        if (!IsFrameworkAssembly(type.Assembly))
        {
            return false;
        }

        return !IsPublic(type) || IsLinqGroupOrLookupClass(type);
    }

    // Older runtimes return these public classes where newer ones return hidden ones. A script only knows them as
    // IGrouping and ILookup: Grouping isn't in the reference assemblies, and ToLookup returns ILookup.
    private static bool IsLinqGroupOrLookupClass(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().FullName is "System.Linq.Grouping`2" or "System.Linq.Lookup`2";

    // Type.IsVisible would also look at generic arguments, and List<T> of a script's internal class isn't hidden.
    private static bool IsPublic(Type type) =>
        type.IsNested ? type.IsNestedPublic && IsPublic(type.DeclaringType!) : type.IsPublic;

    private static bool IsFrameworkAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? string.Empty;

        return name is "System" or "mscorlib" or "netstandard"
               || name.StartsWith("System.", StringComparison.Ordinal)
               || name.StartsWith("Microsoft.", StringComparison.Ordinal);
    }
}
