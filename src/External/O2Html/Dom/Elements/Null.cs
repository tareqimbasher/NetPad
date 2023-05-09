namespace O2Html.Dom.Elements;

public class Null : Element
{
    public Null() : base("span")
    {
        AddText("null");
    }

    public Null(string cssClass) : this()
    {
        this.WithAddClass(cssClass);
    }
}
