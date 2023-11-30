using System.Collections.Generic;
using System.Linq;
using System.Text;
using O2Html.Dom.Attributes;

namespace O2Html.Dom;

public class Element : Node
{
    private readonly List<Node> _children = new();

    public Element(string tagName) : base(NodeType.Element)
    {
        IsSelfClosing = tagName.EndsWith(">");

        tagName = tagName
            .ReplaceIfExists("<", "")
            .ReplaceIfExists(">", "")
            .ReplaceIfExists("/", "");

        TagName = tagName;

        Attributes = new List<ElementAttribute>();
        ClassList = new ClassList(this);
    }

    public string TagName { get; }
    public bool IsSelfClosing { get; }
    public IReadOnlyList<Node> Children => _children;
    public List<ElementAttribute> Attributes { get; }
    public IEnumerable<Element> ChildElements => Children.OfType<Element>();
    public ClassList ClassList { get; }

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

    public void AddText(string? text)
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

        attribute = new ElementAttribute(this, name);
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

    public string InnerHtml(Formatting? formatting = null)
    {
        var output = new List<byte>();

        InnerHtml(output);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    public List<byte> InnerHtml(List<byte> output, Formatting? formatting = null)
    {
        if (IsSelfClosing && Children.Any())
            return output;

        bool addBreaker = formatting == Formatting.NewLines;

        for (var iChild = 0; iChild < Children.Count; iChild++)
        {
            var child = Children[iChild];
            if (addBreaker && iChild > 0) output.Add(HtmlConstants.NewLine);
            child.ToHtml(output, formatting);
        }

        return output;
    }

    public override string ToHtml(Formatting? formatting = null)
    {
        var output = new List<byte>();
        ToHtml(output, formatting);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    public override void ToHtml(List<byte> output, Formatting? formatting = null)
    {
        bool addBreaker = formatting == Formatting.NewLines;

        byte[] tagNameBytes = Encoding.UTF8.GetBytes(TagName);

        output.Add(HtmlConstants.OpeningAngleBracket);
        output.AddRange(tagNameBytes);

        if (Attributes.Count > 0)
        {
            output.Add(HtmlConstants.Space);

            for (var iAttr = 0; iAttr < Attributes.Count; iAttr++)
            {
                var attribute = Attributes[iAttr];

                if (iAttr > 0) output.Add(HtmlConstants.Space);
                output.AddRange(Encoding.UTF8.GetBytes(attribute.ToString()));
            }
        }

        if (IsSelfClosing)
        {
            output.Add(HtmlConstants.ForwardSlash);
            output.Add(HtmlConstants.ClosingAngleBracket);
        }
        else
        {
            output.Add(HtmlConstants.ClosingAngleBracket);

            if (Children.Any())
            {
                if (addBreaker) output.Add(HtmlConstants.NewLine);
                InnerHtml(output, formatting);
                if (addBreaker) output.Add(HtmlConstants.NewLine);
            }

            output.Add(HtmlConstants.OpeningAngleBracket);
            output.Add(HtmlConstants.ForwardSlash);
            output.AddRange(tagNameBytes);
            output.Add(HtmlConstants.ClosingAngleBracket);
        }
    }

    public override string ToString() => ToHtml();
}

public static class HtmlConstants
{
    public const byte OpeningAngleBracket = 0x3C;
    public const byte ClosingAngleBracket = 0x3E;
    public const byte ForwardSlash = 0x2F;
    public const byte Space = 0x20;
    public const byte NewLine = 0x0A;

    //IBufferWriter<>
}
