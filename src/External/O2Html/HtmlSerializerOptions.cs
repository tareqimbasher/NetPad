using System.Collections.Generic;

namespace O2Html;

public class HtmlSerializerOptions
{
    public const ReferenceLoopHandling DefaultReferenceLoopHandling = ReferenceLoopHandling.Error;
    public const uint DefaultMaxDepth = 64;

    private uint _maxDepth = DefaultMaxDepth;

    /// <summary>
    /// How reference loops should be handled. (Default: Error)
    /// </summary>
    public ReferenceLoopHandling ReferenceLoopHandling { get; set; } = DefaultReferenceLoopHandling;

    /// <summary>
    /// If true, empty collections, that are not the root object being serialized, will not be serialized. (Default: false)
    /// </summary>
    public bool DoNotSerializeNonRootEmptyCollections { get; set; }

    /// <summary>
    /// If set, only this number of items will be serialized when serializing collections. (Default: null)
    /// </summary>
    public uint? MaxCollectionSerializeLength { get; set; }

    /// <summary>
    /// The max serialization depth. (Default: 64)
    /// </summary>
    public uint MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value == 0) _maxDepth = DefaultMaxDepth;
            _maxDepth = value;
        }
    }

    /// <summary>
    /// The list of custom HTML Converters to use during serialization.
    /// </summary>
    public List<HtmlConverter> Converters { get; } = new();

    /// <summary>
    /// CSS classes added to serialized HTML nodes.
    /// </summary>
    public CssClasses CssClasses { get; } = new();
}

public class CssClasses
{
    public const string DefaultNullCssClass = "null";
    public const string DefaultPropertyNameClass = "property-name";
    public const string DefaultPropertyValueClass = "property-value";
    public const string DefaultEmptyCollectionCssClass = "empty-collection";
    public const string DefaultCyclicReferenceCssClass = "cyclic-reference";
    public const string DefaultMaxDepthReachedCssClass = "max-depth-reached";
    public const string DefaultTableInfoHeaderCssClass = "table-info-header";
    public const string DefaultTableDataHeaderCssClass = "table-data-header";

    /// <summary>
    /// The CSS class added to null values. (Default: <see cref="DefaultNullCssClass"/>)
    /// </summary>
    public string Null { get; set; } = DefaultNullCssClass;

    /// <summary>
    /// The CSS class added to property names. (Default: <see cref="DefaultPropertyNameClass"/>)
    /// </summary>
    public string PropertyName { get; set; } = DefaultPropertyNameClass;

    /// <summary>
    /// The CSS class added to property values. (Default: <see cref="DefaultPropertyValueClass"/>)
    /// </summary>
    public string PropertyValue { get; set; } = DefaultPropertyValueClass;

    /// <summary>
    /// The CSS class added to empty collections. (Default: <see cref="DefaultEmptyCollectionCssClass"/>)
    /// </summary>
    public string EmptyCollection { get; set; } = DefaultEmptyCollectionCssClass;

    /// <summary>
    /// The CSS class added to cyclic references. (Default: <see cref="DefaultCyclicReferenceCssClass"/>)
    /// </summary>
    public string CyclicReference { get; set; } = DefaultCyclicReferenceCssClass;

    /// <summary>
    /// The CSS class added to max depth reached elements. (Default: <see cref="DefaultMaxDepthReachedCssClass"/>)
    /// </summary>
    public string MaxDepthReached { get; set; } = DefaultMaxDepthReachedCssClass;

    /// <summary>
    /// The CSS class added to a table's info header. (Default: <see cref="DefaultTableInfoHeaderCssClass"/>)
    /// </summary>
    public string TableInfoHeader { get; set; } = DefaultTableInfoHeaderCssClass;

    /// <summary>
    /// The CSS class added to a table's data header. (Default: <see cref="DefaultTableDataHeaderCssClass"/>)
    /// </summary>
    public string TableDataHeader { get; set; } = DefaultTableDataHeaderCssClass;
}
