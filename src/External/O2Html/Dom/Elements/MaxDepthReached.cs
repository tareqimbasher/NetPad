namespace O2Html.Dom.Elements;

public class MaxDepthReached : Element
{
    public MaxDepthReached(string cssClass) : base("span")
    {
        AddText("max depth reached");
        this.WithAddClass(cssClass);
    }
}
