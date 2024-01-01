using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using O2Html.Common;
using O2Html.Dom.Attributes;

namespace O2Html.Dom;

/// <summary>
/// A HTML Element-type Node.
/// </summary>
public class Element : Node
{
    private readonly List<Node> _children = new();

    /// <summary>
    /// Creates a new <see cref="Element"/>.
    /// </summary>
    /// <param name="tagName">The tag name of the element.</param>
    ///
    public Element(string tagName) : base(NodeType.Element)
    {
        if (tagName == null) throw new ArgumentNullException(nameof(tagName));

        if (HtmlConsts.SelfClosingTags.Contains(tagName))
        {
            IsSelfClosing = true;
        }
        else
        {
            IsSelfClosing = tagName.EndsWith(">");

            tagName = tagName
                .ReplaceIfExists("<", "")
                .ReplaceIfExists(">", "")
                .ReplaceIfExists("/", "");
        }

        TagName = tagName.ToLowerInvariant();

        Attributes = new List<ElementAttribute>();
        ClassList = new ClassList(this);
    }

    /// <summary>
    /// The element's tag name
    /// </summary>
    public string TagName { get; }

    /// <summary>
    /// Whether this element is self-closing. If an element is self-closing it cannot contain children.
    /// </summary>
    public bool IsSelfClosing { get; }

    /// <summary>
    /// HTML attributes added to this element.
    /// </summary>
    public List<ElementAttribute> Attributes { get; }

    /// <summary>
    /// List of CSS classes applied to this element.
    /// </summary>
    public ClassList ClassList { get; }

    /// <summary>
    /// All child nodes.
    /// </summary>
    public IReadOnlyList<Node> Children => _children;

    /// <summary>
    /// Element-type child nodes.
    /// </summary>
    public IEnumerable<Element> ChildElements => Children.OfType<Element>();

    public bool HasAttribute(string name)
    {
        return Attributes.Any(a => a.Name == name);
    }

    public ElementAttribute? GetAttribute(string name)
    {
        return Attributes.FirstOrDefault(a => a.Name == name);
    }

    public ElementAttribute GetOrAddAttribute(string name)
    {
        var attribute = GetAttribute(name);
        if (attribute != null) return attribute;

        attribute = new ElementAttribute(this, name);
        Attributes.Add(attribute);

        return attribute;
    }

    public ElementAttribute? DeleteAndGetAttribute(string name)
    {
        var attribute = GetAttribute(name);
        if (attribute != null)
            Attributes.Remove(attribute);
        return attribute;
    }

    internal void InternalAddChild(Node child)
    {
        EnsureCanManageChildren();

        if (child.Parent == this)
        {
            return;
        }

        child.Parent = this;
        _children.Add(child);
    }

    public void InsertChild(int index, Node child)
    {
        EnsureCanManageChildren();

        if (child.Parent == this)
        {
            return;
        }

        child.Parent = this;
        _children.Insert(index, child);
    }

    public void RemoveChild(Node child)
    {
        EnsureCanManageChildren();

        if (child.Parent != this)
        {
            return;
        }

        _children.Remove(child);
        child.Parent = null;
    }

    public void Clear()
    {
        EnsureCanManageChildren();

        var children = _children.ToList();

        _children.Clear();
        children.ForEach(c => c.Parent = null);
    }

    private void EnsureCanManageChildren()
    {
        if (IsSelfClosing)
        {
            throw new InvalidOperationException("Cannot add or remove children in a self-closing element.");
        }
    }

    public string InnerHtml(Formatting? formatting = null, int indentLevel = 0)
    {
        var buffer = new List<byte>();

        InnerHtml(buffer, formatting, indentLevel);

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public List<byte> InnerHtml(List<byte> buffer, Formatting? formatting = null, int indentLevel = 0)
    {
        if (IsSelfClosing && Children.Any())
            return buffer;

        bool indented = formatting == Formatting.Indented;

        for (var iChild = 0; iChild < Children.Count; iChild++)
        {
            var child = Children[iChild];
            if (indented && iChild > 0) buffer.Add(HtmlConsts.NewLineByte);
            child.ToHtml(buffer, formatting, indentLevel);
        }

        return buffer;
    }

    public override string ToHtml(Formatting? formatting = null, int indentLevel = 0)
    {
        var buffer = new List<byte>();
        ToHtml(buffer, formatting, indentLevel);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public override void ToHtml(List<byte> buffer, Formatting? formatting = null, int indentLevel = 0)
    {
        bool indented = formatting == Formatting.Indented;

        byte[] tagNameBytes = Encoding.UTF8.GetBytes(TagName);

        if (indented) buffer.AddIndent(indentLevel);
        buffer.Add(HtmlConsts.OpeningAngleBracketByte);
        buffer.AddRange(tagNameBytes);

        if (Attributes.Count > 0)
        {
            buffer.Add(HtmlConsts.SpaceByte);

            for (var iAttr = 0; iAttr < Attributes.Count; iAttr++)
            {
                var attribute = Attributes[iAttr];

                if (iAttr > 0) buffer.Add(HtmlConsts.SpaceByte);
                buffer.AddRange(Encoding.UTF8.GetBytes(attribute.ToString()));
            }
        }

        if (IsSelfClosing)
        {
            buffer.Add(HtmlConsts.ForwardSlashByte);
            buffer.Add(HtmlConsts.ClosingAngleBracketByte);
        }
        else
        {
            buffer.Add(HtmlConsts.ClosingAngleBracketByte);

            if (Children.Any())
            {
                if (indented) buffer.Add(HtmlConsts.NewLineByte);
                InnerHtml(buffer, formatting, indentLevel + 1);
            }

            if (indented && Children.Any())
            {
                buffer.Add(HtmlConsts.NewLineByte);
                buffer.AddIndent(indentLevel);
            }

            buffer.Add(HtmlConsts.OpeningAngleBracketByte);
            buffer.Add(HtmlConsts.ForwardSlashByte);
            buffer.AddRange(tagNameBytes);
            buffer.Add(HtmlConsts.ClosingAngleBracketByte);
        }
    }

    public override string ToString() => ToHtml();
}
