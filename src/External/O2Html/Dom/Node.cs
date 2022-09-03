using System.Collections.Generic;

namespace O2Html.Dom;

public abstract class Node
{
    protected Node(NodeType type)
    {
        Type = type;
    }

    public NodeType Type { get; }
    public Element? Parent { get; internal set; }

    public abstract string ToHtml(Formatting? formatting = null);
    public abstract void ToHtml(List<byte> output, Formatting? formatting = null);

    public void Delete()
    {
        Parent?.RemoveChild(this);
    }
}
