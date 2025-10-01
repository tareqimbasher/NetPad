namespace NetPad.Common;

/// <summary>
/// Resolves type names to runtime types and vice versa.
/// </summary>
public interface ITypeNameResolver
{
    string GetName(Type type);
    Type? Resolve(string name);
}

/// <summary>
/// Uses the runtime type's full name for type resolution.
/// </summary>
public sealed class FullNameTypeResolver : ITypeNameResolver
{
    public string GetName(Type type) => type.FullName ?? type.Name;
    public Type? Resolve(string name) => Type.GetType(name, throwOnError: false);
}

/// <summary>
/// Uses the runtime type's assembly qualified full name for type resolution.
/// </summary>
public sealed class AssemblyQualifiedNameTypeResolver : ITypeNameResolver
{
    public string GetName(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
    public Type? Resolve(string name) => Type.GetType(name, throwOnError: false);
}
