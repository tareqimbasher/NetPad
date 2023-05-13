using System.Collections.Generic;

namespace O2Html;

public class HtmlSerializerSettings
{
    internal const ReferenceLoopHandling DefaultReferenceLoopHandling = ReferenceLoopHandling.Error;

    public HtmlSerializerSettings()
    {
        Converters = new List<HtmlConverter>();
        CssClasses = new CssClasses();
    }

    public ReferenceLoopHandling ReferenceLoopHandling { get; set; } = DefaultReferenceLoopHandling;

    public List<HtmlConverter> Converters { get; }
    public CssClasses CssClasses { get; }

    /// <summary>
    /// If true, will not serialize empty collections that are not the root object being serialized. Default is false.
    /// </summary>
    public bool DoNotSerializeNonRootEmptyCollections { get; set; }

    /// <summary>
    /// If set, only this number of items will be serialized when serializing collections.
    /// </summary>
    public int? MaxCollectionSerializeLength { get; set; }
}

public class CssClasses
{
    internal const string DefaultNullCssClass = "null";
    internal const string DefaultPropertyNameClass = "property-name";
    internal const string DefaultPropertyValueClass = "property-value";
    internal const string DefaultEmptyCollectionCssClass = "empty-collection";
    internal const string DefaultCyclicReferenceCssClass = "cyclic-reference";
    internal const string DefaultTableInfoHeaderCssClass = "table-info-header";
    internal const string DefaultTableDataHeaderCssClass = "table-data-header";

    /// <summary>
    /// The CSS class added to null values.
    /// </summary>
    public string Null { get; set; } = DefaultNullCssClass;

    /// <summary>
    /// The CSS class added to property names.
    /// </summary>
    public string PropertyName { get; set; } = DefaultPropertyNameClass;

    /// <summary>
    /// The CSS class added to property values.
    /// </summary>
    public string PropertyValue { get; set; } = DefaultPropertyValueClass;

    /// <summary>
    /// The CSS class added to empty collections.
    /// </summary>
    public string EmptyCollection { get; set; } = DefaultEmptyCollectionCssClass;

    /// <summary>
    /// The CSS class added to cyclic references.
    /// </summary>
    public string CyclicReference { get; set; } = DefaultCyclicReferenceCssClass;

    /// <summary>
    /// The CSS class added to a table's info header.
    /// </summary>
    public string TableInfoHeader { get; set; } = DefaultTableInfoHeaderCssClass;

    /// <summary>
    /// The CSS class added to a table's data header.
    /// </summary>
    public string TableDataHeader { get; set; } = DefaultTableDataHeaderCssClass;
}
