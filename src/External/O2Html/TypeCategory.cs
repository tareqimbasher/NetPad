namespace O2Html;

/// <summary>
/// A high-level category of .NET types.
/// </summary>
public enum TypeCategory
{
    /// <summary>
    /// Types that should be represented as a string.
    /// </summary>
    DotNetTypeWithStringRepresentation = 0,

    /// <summary>
    /// A type that represents a single object or value type, not a collection.
    /// </summary>
    SingleObject = 1,

    /// <summary>
    /// A type that represents a collection of objects or value types.
    /// </summary>
    Collection = 2
}
