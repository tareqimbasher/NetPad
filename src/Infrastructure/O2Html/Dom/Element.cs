using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace O2Html.Dom;

public class Element : Node
{
    private readonly List<Node> _children = new();

    public Element(string tagName) : base(NodeType.Element)
    {
        IsSelfClosing = tagName.EndsWith(">");

        TagName = tagName
            .Replace("<", "")
            .Replace(">", "")
            .Replace("/", "");
        Attributes = new List<ElementAttribute>();
    }

    public string TagName { get; }
    public bool IsSelfClosing { get; }
    public IReadOnlyList<Node> Children => _children;
    public List<ElementAttribute> Attributes { get; }
    public IEnumerable<Element> ChildElements => Children.OfType<Element>();


    public void AddChild(Node child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public void InsertChild(int index, Node child)
    {
        child.Parent = this;
        _children.Insert(index, child);
    }

    public void RemoveChild(Node node)
    {
        if (!_children.Contains(node)) return;

        _children.Remove(node);
        node.Parent = null;
    }

    public void AddElement(string tagName)
    {
        var element = new Element(tagName);
        AddChild(element);
    }

    public void AddText(string text)
    {
        var textNode = new TextNode(text);
        AddChild(textNode);
    }

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

        attribute = new ElementAttribute(this, name, null);
        Attributes.Add(attribute);

        return attribute;
    }

    public ElementAttribute SetOrAddAttribute(string name, string? value)
    {
        return GetOrAddAttribute(name).Set(value);
    }

    public ElementAttribute? DeleteAttribute(string name)
    {
        var attribute = GetAttribute(name);
        if (attribute != null)
            Attributes.Remove(attribute);
        return attribute;
    }

    public void Clear()
    {
        _children.Clear();
    }

    public string? InnerHtml(Formatting? formatting = null)
    {
        if (IsSelfClosing)
            return null;

        return string.Join(formatting == Formatting.NewLines ? "\n" : string.Empty, Children.Select(c => c.ToHtml(formatting)));
    }

    public override string ToHtml(Formatting? formatting = null)
    {
        string breaker = formatting == Formatting.NewLines ? "\n" : string.Empty;

        var sb = new StringBuilder()
            .Append('<')
            .Append(TagName)
            .Append(Attributes.Any() ? " " : "")
            .Append(string.Join(" ", Attributes.Select(a => a.ToString())))
            .Append(IsSelfClosing ? "/>" : ">");

        if (!IsSelfClosing)
        {
            if (Children.Any())
            {
                sb.Append(breaker)
                    .Append(InnerHtml(formatting))
                    .Append(breaker);
            }

            sb.Append($"</{TagName}>");
        }

        return sb.ToString();
    }

    public override string ToString() => ToHtml();
}
