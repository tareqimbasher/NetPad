using System.Collections.Generic;
using System.Text;

namespace O2Html.Dom;

public class TextNode : Node
{
    public TextNode(string? text) : base(NodeType.Text)
    {
        Text = text;
    }

    public string? Text { get; private set; }

    public void SetText(string? text)
    {
        Text = text;
    }

    public override string ToHtml(Formatting? formatting = null)
    {
        var output = new List<byte>();
        ToHtml(output, formatting);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    public override void ToHtml(List<byte> output, Formatting? formatting = null)
    {
        if (!string.IsNullOrEmpty(Text))
            output.AddRange(Encoding.UTF8.GetBytes(Text));
    }

    public override string ToString() => ToHtml();
}
