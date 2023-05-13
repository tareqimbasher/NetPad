namespace O2Html;

/// <summary>
/// A high-level category of .NET types
/// </summary>
public enum TypeCategory
{
    /// <summary>
    /// Types that can be represented as a string.
    /// </summary>
    DotNetTypeWithStringRepresentation = 0,

    /// <summary>
    /// A type that represents a single (not a collection) object.
    /// </summary>
    SingleObject = 1,

    /// <summary>
    /// A type that represents a collection of objects.
    /// </summary>
    Collection = 2
}
