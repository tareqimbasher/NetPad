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
    internal THead() : base("thead")
    {
    }

    public THead AddHeading(string text, string? title = null)
    {
        AddAndGetHeading(text, title);
        return this;
    }

    public Element AddAndGetHeading(string text, string? title = null)
    {
        var row = ChildElements.FirstOrDefault() ?? AddAndGetRow();

        var heading = row.AddAndGetElement("th");

        if (title != null)
            heading.SetTitle(title);

        return heading.AddEscapedText(text);
    }
}

public class TBody : ElementWithTableRows
{
    internal TBody() : base("tbody")
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
