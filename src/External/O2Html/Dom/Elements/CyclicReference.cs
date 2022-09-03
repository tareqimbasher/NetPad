using System;

namespace O2Html.Dom.Elements;

public class CyclicReference : Element
{
    public CyclicReference(Type type) : base("div")
    {
        AddText($"Cyclic reference ({type.GetReadableName(withNamespace: true, forHtml: true)})");
    }
}
