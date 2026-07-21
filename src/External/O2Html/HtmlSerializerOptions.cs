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
    public const string DefaultItemCountCssClass = "item-count";
    public const string DefaultNumericCssClass = "numeric";
    public const string DefaultBooleanTrueCssClass = "boolean-true";
    public const string DefaultBooleanFalseCssClass = "boolean-false";
    public const string DefaultEnumCssClass = "enum";
    public const string DefaultTemporalCssClass = "temporal";
    public const string DefaultNegativeCssClass = "negative";

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

    /// <summary>
    /// The CSS class added to the number of items a table is showing. (Default: <see cref="DefaultItemCountCssClass"/>)
    /// </summary>
    public string ItemCount { get; set; } = DefaultItemCountCssClass;

    /// <summary>
    /// The CSS class added to value cells holding a number. (Default: <see cref="DefaultNumericCssClass"/>)
    /// </summary>
    public string Numeric { get; set; } = DefaultNumericCssClass;

    /// <summary>
    /// The CSS class added to value cells holding <c>true</c>. (Default: <see cref="DefaultBooleanTrueCssClass"/>)
    /// </summary>
    public string BooleanTrue { get; set; } = DefaultBooleanTrueCssClass;

    /// <summary>
    /// The CSS class added to value cells holding <c>false</c>. (Default: <see cref="DefaultBooleanFalseCssClass"/>)
    /// </summary>
    public string BooleanFalse { get; set; } = DefaultBooleanFalseCssClass;

    /// <summary>
    /// The CSS class added to value cells holding an enum member. (Default: <see cref="DefaultEnumCssClass"/>)
    /// </summary>
    public string Enum { get; set; } = DefaultEnumCssClass;

    /// <summary>
    /// The CSS class added to value cells holding a date, time or duration. (Default: <see cref="DefaultTemporalCssClass"/>)
    /// </summary>
    public string Temporal { get; set; } = DefaultTemporalCssClass;

    /// <summary>
    /// The CSS class added to numeric value cells holding a value below zero. (Default: <see cref="DefaultNegativeCssClass"/>)
    /// </summary>
    public string Negative { get; set; } = DefaultNegativeCssClass;
}
