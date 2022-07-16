namespace O2Html.Dom.Elements;

public class CyclicReference : Element
{
    public CyclicReference(object? obj = null) : base("div")
    {
        if (obj != null)
        {
            AddText($"Cyclic reference ({obj.GetType().GetReadableName(withNamespace: true, forHtml: true)})");
        }
    }
}
