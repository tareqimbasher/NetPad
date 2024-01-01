namespace O2Html.Dom.Elements;

public class Null : Element
{
    public Null() : base("span")
    {
        this.AddText("null");
    }

    public Null(string cssClass) : this()
    {
        this.AddClass(cssClass);
    }
}
