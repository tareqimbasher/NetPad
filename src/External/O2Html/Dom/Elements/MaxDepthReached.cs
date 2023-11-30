namespace O2Html.Dom.Elements;

public class MaxDepthReached : Element
{
    public MaxDepthReached(string cssClass) : base("span")
    {
        this.AddText("max depth reached");
        this.AddClass(cssClass);
    }
}
