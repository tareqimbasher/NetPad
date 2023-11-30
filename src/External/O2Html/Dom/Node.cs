using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace O2Html.Dom;

/// <summary>
/// An HTML DOM Node. This is base class for all HTML DOM representations.
/// </summary>
public abstract class Node
{
    protected Node(NodeType type)
    {
        Type = type;
    }

    public NodeType Type { get; }
    public Element? Parent { get; internal set; }

    /// <summary>
    /// Serializes this Node and its subtree to an HTML string.
    /// </summary>
    /// <param name="formatting">Formatting of serialized HTML string.</param>
    /// <param name="indentLevel">Writes HTML at this indentation level.</param>
    [Pure]
    public abstract string ToHtml(Formatting? formatting = null, int indentLevel = 0);

    /// <summary>
    /// Serializes this Node and its subtree and the serialized HTML string as bytes to buffer.
    /// </summary>
    /// <param name="buffer">The buffer to add to.</param>
    /// <param name="formatting">Formatting of serialized HTML string.</param>
    /// <param name="indentLevel">Writes HTML at this indentation level.</param>
    public abstract void ToHtml(List<byte> buffer, Formatting? formatting = null, int indentLevel = 0);

    /// <summary>
    /// Removes this node from its <see cref="Parent"/>.
    /// </summary>
    public void Remove()
    {
        Parent?.RemoveChild(this);
    }

    /// <summary>
    /// Alias to <see cref="ToHtml(System.Nullable{O2Html.Formatting},int)"/>.
    /// </summary>
    public override string ToString()
    {
        return ToHtml();
    }
}
