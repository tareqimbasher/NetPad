using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace O2Html.Dom;

public abstract class Node
{
    protected Node(NodeType type)
    {
        Type = type;
    }

    public NodeType Type { get; }
    public Element? Parent { get; internal set; }

    [Pure]
    public abstract string ToHtml(Formatting? formatting = null);

    public abstract void ToHtml(List<byte> output, Formatting? formatting = null);

    public void Delete()
    {
        Parent?.RemoveChild(this);
    }

    /// <summary>
    /// Alias to <see cref="ToHtml(System.Nullable{O2Html.Formatting})"/>.
    /// </summary>
    public override string ToString()
    {
        return ToHtml();
    }
}
