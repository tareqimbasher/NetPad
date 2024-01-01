using System;

namespace O2Html.Dom.Elements;

public class CyclicReference : Element
{
    public CyclicReference(Type type) : base("div")
    {
        this.AddEscapedText($"Cyclic reference ({type.GetReadableName(true)})");
    }
}
