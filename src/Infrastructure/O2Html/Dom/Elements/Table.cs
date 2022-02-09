using System.Linq;

namespace O2Html.Dom.Elements;

public class Table : Element
{
    public Table() : base("table")
    {
        Head = this.AddAndGetElement("thead");
        Body = this.AddAndGetElement("tbody");
    }

    public Element Head { get; set; }

    public Element Body { get; set; }

    public void AddHeading(string headingText, string? title = null)
    {
        AddAndGetHeading(headingText, title);
    }

    public Element AddAndGetHeading(string headingText, string? title = null)
    {
        var tr = GetHeaderRow();
        var th = tr.AddAndGetElement("th");

        if (title != null)
            th.WithTitle(title);

        return th.AddAndGetElement("a").WithText(headingText);
    }

    private Element GetHeaderRow()
    {
        if (!Head.Children.Any())
            return Head.AddAndGetElement("tr");

        return  Head.ChildElements.First();
    }
}
