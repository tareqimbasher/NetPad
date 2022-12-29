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
}

public class CssClasses
{
    internal const string DefaultNullCssClass = "null";
    internal const string DefaultPropertyNameClass = "property-name";
    internal const string DefaultPropertyValueClass = "property-value";
    internal const string DefaultEmptyCollectionCssClass = "empty-collection";
    internal const string DefaultCyclicReferenceCssClass = "cyclic-reference";

    /// <summary>
    /// The CSS class that null values will have
    /// </summary>
    public string Null { get; set; } = DefaultNullCssClass;
    /// <summary>
    /// The CSS class that property names will have
    /// </summary>
    public string PropertyName { get; set; } = DefaultPropertyNameClass;
    /// <summary>
    /// The CSS class that property values will have
    /// </summary>
    public string PropertyValue { get; set; } = DefaultPropertyValueClass;
    /// <summary>
    /// The CSS class that empty collections will have
    /// </summary>
    public string EmptyCollection { get; set; } = DefaultEmptyCollectionCssClass;
    /// <summary>
    /// The CSS class that cyclic references will have
    /// </summary>
    public string CyclicReference { get; set; } = DefaultCyclicReferenceCssClass;
}
