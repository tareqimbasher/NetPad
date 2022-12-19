namespace O2Html.Dom.Elements;

public class EmptyCollection : Element
{
    public EmptyCollection() : base("span")
    {
        AddText("0 items");
    }
}
