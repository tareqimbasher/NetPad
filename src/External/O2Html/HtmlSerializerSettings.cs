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
    public bool DoNotSerializeNonRootEmptyCollections { get; set; } = false;
}

public class CssClasses
{
    internal const string DefaultNullCssClass = "null";
    internal const string DefaultPropertyNameClass = "property-name";
    internal const string DefaultCyclicReferenceCssClass = "cyclic-reference";
    internal const string DefaultEmptyCollectionCssClass = "empty-collection";

    public string Null { get; set; } = DefaultNullCssClass;
    public string PropertyName { get; set; } = DefaultPropertyNameClass;
    public string CyclicReference { get; set; } = DefaultCyclicReferenceCssClass;
    public string EmptyCollection { get; set; } = DefaultEmptyCollectionCssClass;
}
