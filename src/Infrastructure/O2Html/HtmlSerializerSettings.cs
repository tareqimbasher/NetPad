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
}

public class CssClasses
{
    internal const string DefaultNullCssClass = "null";
    internal const string DefaultPropertyNameClass = "property-name";
    internal const string DefaultTableCssClass = "table table-sm table-bordered";
    internal const string DefaultCyclicReferenceCssClass = "cyclic-reference";

    public string Null { get; set; } = DefaultNullCssClass;
    public string PropertyName { get; set; } = DefaultPropertyNameClass;
    public string Table { get; set; } = DefaultTableCssClass;
    public string CyclicReference { get; set; } = DefaultCyclicReferenceCssClass;
}
