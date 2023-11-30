using System;

namespace O2Html.Dom.Elements;

public class EmptyCollection : Element
{
    public EmptyCollection() : base("span")
    {
        this.AddText("0 items");
    }

    public EmptyCollection(Type collectionType) : base("span")
    {
        this.AddEscapedText($"0 items ({collectionType.GetReadableName()})");
    }
}
