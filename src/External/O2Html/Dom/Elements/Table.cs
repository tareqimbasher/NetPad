using System.Linq;

namespace O2Html.Dom.Elements;

public abstract class ElementWithTableRows : Element
{
    protected ElementWithTableRows(string tagName) : base(tagName)
    {
    }

    public Element AddAndGetRow()
    {
        return this.AddAndGetElement("tr");
    }
}

public class THead : ElementWithTableRows
{
    public THead() : base("thead")
    {
    }

    public THead WithHeading(string text, string? title = null)
    {
        AddAndGetHeading(text, title);
        return this;
    }

    public Element AddAndGetHeading(string text, string? title = null)
    {
        var row = ChildElements.FirstOrDefault() ?? AddAndGetRow();

        var heading = row.AddAndGetElement("th");

        if (title != null)
            heading.WithTitle(title);

        return heading.WithText(text);
    }
}

public class TBody : ElementWithTableRows
{
    public TBody() : base("tbody")
    {
    }
}

public class Table : Element
{
    public Table() : base("table")
    {
        Head = this.AddAndGetChild(new THead());
        Body = this.AddAndGetChild(new TBody());
    }

    public THead Head { get; }

    public TBody Body { get; }
}
